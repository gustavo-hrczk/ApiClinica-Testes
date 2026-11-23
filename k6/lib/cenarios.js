// Cenarios de uso exercitados pelos testes de carga.
import http from 'k6/http';
import exec from 'k6/execution';
import { check, group, sleep } from 'k6';
import { BASE_URL, cpfUnico, espera, headers, horarioDaIteracao, novoPaciente } from './api.js';

// A tag "name" agrupa as URLs com id (senao cada /pacientes/123 viraria uma metrica).
// A tag "tipo" separa leitura de escrita, para thresholds e analise distintos.
const leitura = (token, name) => ({ headers: headers(token), tags: { name, tipo: 'leitura' } });
const escrita = (token, name) => ({ headers: headers(token), tags: { name, tipo: 'escrita' } });

/** Pausa entre as etapas, simulando o tempo que a pessoa leva na tela (0,5 a 1,5 s). */
function pensar() {
  sleep(0.5 + Math.random());
}

/**
 * Jornada de uma recepcionista da clinica, cobrindo o CRUD completo:
 * consulta as listagens, cadastra um paciente, agenda e reagenda uma consulta
 * e, por fim, cancela a consulta e remove o cadastro.
 * Sao 11 requisicoes por iteracao: 5 leituras e 6 escritas.
 */
export function jornadaRecepcao(dados) {
  const token = dados.token;
  const vu = exec.vu.idInTest;
  const iteracao = exec.vu.iterationInInstance;
  // Cada VU usa o proprio medico e um horario por iteracao: nenhum 409 e esperado.
  const medicoId = dados.medicos[(vu - 1) % dados.medicos.length];

  let pacienteId = null;
  let consultaId = null;

  group('1. consultar listagens', () => {
    const pacientes = http.get(`${BASE_URL}/api/pacientes`, leitura(token, 'GET /api/pacientes'));
    check(pacientes, {
      'listar pacientes: 200': (r) => r.status === 200,
      'listar pacientes: devolve array': (r) => Array.isArray(r.json()),
    });

    const medicos = http.get(`${BASE_URL}/api/medicos`, leitura(token, 'GET /api/medicos'));
    check(medicos, { 'listar medicos: 200': (r) => r.status === 200 });

    const consultas = http.get(`${BASE_URL}/api/consultas`, leitura(token, 'GET /api/consultas'));
    check(consultas, { 'listar consultas: 200': (r) => r.status === 200 });
  });

  pensar();

  group('2. cadastrar paciente', () => {
    const cpf = cpfUnico(vu, iteracao);

    const criado = http.post(
      `${BASE_URL}/api/pacientes`,
      JSON.stringify(novoPaciente(cpf)),
      escrita(token, 'POST /api/pacientes')
    );
    const ok = check(criado, {
      'criar paciente: 201': (r) => r.status === 201,
      'criar paciente: tem id': (r) => r.status === 201 && r.json('id') > 0,
      'criar paciente: nao expoe cpf': (r) => r.status === 201 && r.json('cpf') === undefined,
    });
    if (!ok) return;
    pacienteId = criado.json('id');

    const buscado = http.get(
      `${BASE_URL}/api/pacientes/${pacienteId}`,
      leitura(token, 'GET /api/pacientes/{id}')
    );
    check(buscado, {
      'buscar paciente: 200': (r) => r.status === 200,
      'buscar paciente: id confere': (r) => r.status === 200 && r.json('id') === pacienteId,
    });

    const atualizado = http.patch(
      `${BASE_URL}/api/pacientes/${pacienteId}`,
      JSON.stringify({ Telefone: '(47) 3333-4444' }),
      escrita(token, 'PATCH /api/pacientes/{id}')
    );
    check(atualizado, {
      'atualizar paciente: 200': (r) => r.status === 200,
      'atualizar paciente: telefone mudou': (r) =>
        r.status === 200 && r.json('telefone') === '(47) 3333-4444',
    });
  });

  pensar();

  if (pacienteId !== null) {
    group('3. agendar consulta', () => {
      const criada = http.post(
        `${BASE_URL}/api/consultas`,
        JSON.stringify({
          PacienteId: pacienteId,
          MedicoId: medicoId,
          DataHora: horarioDaIteracao(iteracao),
        }),
        escrita(token, 'POST /api/consultas')
      );
      const ok = check(criada, {
        'criar consulta: 201': (r) => r.status === 201,
        'criar consulta: tem id': (r) => r.status === 201 && r.json('id') > 0,
      });
      if (!ok) return;
      consultaId = criada.json('id');

      const buscada = http.get(
        `${BASE_URL}/api/consultas/${consultaId}`,
        leitura(token, 'GET /api/consultas/{id}')
      );
      check(buscada, {
        'buscar consulta: 200': (r) => r.status === 200,
        'buscar consulta: paciente confere': (r) =>
          r.status === 200 && r.json('pacienteId') === pacienteId,
      });

      const reagendada = http.patch(
        `${BASE_URL}/api/consultas/${consultaId}`,
        JSON.stringify({ DataHora: horarioDaIteracao(iteracao, 30) }),
        escrita(token, 'PATCH /api/consultas/{id}')
      );
      check(reagendada, { 'reagendar consulta: 204': (r) => r.status === 204 });
    });

    pensar();
  }

  group('4. cancelar e remover', () => {
    if (consultaId !== null) {
      const r = http.del(
        `${BASE_URL}/api/consultas/${consultaId}`,
        null,
        escrita(token, 'DELETE /api/consultas/{id}')
      );
      check(r, { 'excluir consulta: 204': (res) => res.status === 204 });
    }
    if (pacienteId !== null) {
      const r = http.del(
        `${BASE_URL}/api/pacientes/${pacienteId}`,
        null,
        escrita(token, 'DELETE /api/pacientes/{id}')
      );
      check(r, { 'excluir paciente: 204': (res) => res.status === 204 });
    }
  });
}

/**
 * Verificacoes de contrato (cenarios de erro), usadas so no smoke test:
 * confirmam que a API responde com o codigo certo quando a requisicao e invalida.
 * Os 4xx aqui sao esperados e, por isso, nao entram em http_req_failed.
 */
export function verificacoesDeErro(dados) {
  const token = dados.token;
  const com = (name, ...status) => ({
    headers: headers(token),
    tags: { name, tipo: 'erro-esperado' },
    responseCallback: espera(...status),
  });

  group('5. respostas de erro', () => {
    const semToken = http.get(`${BASE_URL}/api/pacientes`, {
      tags: { name: 'GET /api/pacientes (sem token)', tipo: 'erro-esperado' },
      responseCallback: espera(401),
    });
    check(semToken, { 'sem token: 401': (r) => r.status === 401 });

    const inexistente = http.get(
      `${BASE_URL}/api/pacientes/999999999`,
      com('GET /api/pacientes/{id} (inexistente)', 404)
    );
    check(inexistente, { 'paciente inexistente: 404': (r) => r.status === 404 });

    const cpfInvalido = http.post(
      `${BASE_URL}/api/pacientes`,
      JSON.stringify(novoPaciente('11111111111')),
      com('POST /api/pacientes (cpf invalido)', 400)
    );
    check(cpfInvalido, { 'cpf invalido: 400': (r) => r.status === 400 });

    const emailInvalido = http.post(
      `${BASE_URL}/api/medicos`,
      JSON.stringify({ Nome: 'X', Email: 'sem-arroba', Telefone: '(47) 97777-7777', CRM: '1' }),
      com('POST /api/medicos (email invalido)', 400)
    );
    check(emailInvalido, { 'email invalido: 400': (r) => r.status === 400 });

    // CPF ja usado por um paciente da massa inicial criada no setup.
    const duplicado = http.post(
      `${BASE_URL}/api/pacientes`,
      JSON.stringify(novoPaciente(cpfUnico(0, 0))),
      com('POST /api/pacientes (cpf duplicado)', 409)
    );
    check(duplicado, { 'cpf duplicado: 409': (r) => r.status === 409 });

    const noPassado = http.post(
      `${BASE_URL}/api/consultas`,
      JSON.stringify({
        PacienteId: dados.pacientes[0],
        MedicoId: dados.medicos[0],
        DataHora: '2020-01-01T10:00:00',
      }),
      com('POST /api/consultas (no passado)', 400)
    );
    check(noPassado, { 'consulta no passado: 400': (r) => r.status === 400 });
  });
}
