// Funcoes compartilhadas pelos tres testes de carga (smoke, average-load e stress).
import http from 'k6/http';
import { check, fail } from 'k6';

export const BASE_URL = __ENV.BASE_URL || 'http://localhost:5052';

const ADMIN = { Login: 'k6-admin', Senha: 'k6-admin-123', Perfil: 'admin' };
const JSON_HEADERS = { 'Content-Type': 'application/json' };

// Respostas 4xx provocadas de proposito nao contam como falha em http_req_failed.
export const espera = (...status) => http.expectedStatuses(...status);

export function headers(token) {
  return { ...JSON_HEADERS, Authorization: `Bearer ${token}` };
}

// ---------- geracao de dados ----------

/** Calcula os dois digitos verificadores e devolve um CPF valido de 11 digitos. */
export function cpfValido(base9) {
  const d = String(base9).padStart(9, '0').split('').map(Number);
  const digito = (qtd) => {
    let soma = 0;
    for (let i = 0; i < qtd; i++) soma += d[i] * (qtd + 1 - i);
    const resto = soma % 11;
    return resto < 2 ? 0 : 11 - resto;
  };
  d.push(digito(9));
  d.push(digito(10));
  return d.join('');
}

/**
 * CPF unico por (VU, iteracao): 3 digitos do VU + 6 da iteracao.
 * O VU 0 e o setup(), usado para os pacientes de massa inicial.
 */
export function cpfUnico(vu, iteracao) {
  return cpfValido((vu % 1000) * 1000000 + ((iteracao + 1) % 1000000));
}

/** Data local no formato aceito pela API (sem "Z"): 2026-12-10T10:00:00 */
export function dataLocal(date) {
  const p = (n) => String(n).padStart(2, '0');
  return (
    `${date.getFullYear()}-${p(date.getMonth() + 1)}-${p(date.getDate())}` +
    `T${p(date.getHours())}:${p(date.getMinutes())}:00`
  );
}

/** Horario futuro e exclusivo de cada iteracao: amanha 00:00 + iteracao horas. */
export function horarioDaIteracao(iteracao, minutosExtras = 0) {
  const d = new Date();
  d.setHours(0, 0, 0, 0);
  d.setDate(d.getDate() + 1);
  d.setMinutes(iteracao * 60 + minutosExtras);
  return dataLocal(d);
}

export function novoPaciente(cpf) {
  return {
    Nome: `Paciente k6 ${cpf}`,
    Email: `paciente.${cpf}@example.com`,
    Telefone: '(47) 98888-8888',
    DataNasc: '1995-03-24',
    Cpf: cpf,
  };
}

// ---------- setup / teardown ----------

/**
 * Prepara o ambiente antes da carga:
 *  1. garante o usuario admin e faz login (o token vale 2 horas);
 *  2. cria um medico por VU, para que as agendas nao colidam entre VUs;
 *  3. cria uma massa inicial de pacientes, para as listagens terem volume realista.
 */
export function preparar(qtdMedicos, qtdPacientesIniciais) {
  // 200 = criado agora; 400 = ja existia de uma execucao anterior.
  const registro = http.post(`${BASE_URL}/api/auth/register`, JSON.stringify(ADMIN), {
    headers: JSON_HEADERS,
    responseCallback: espera(200, 400),
  });
  if (registro.status !== 200 && registro.status !== 400) {
    fail(`API indisponivel em ${BASE_URL} (register devolveu ${registro.status})`);
  }

  const login = http.post(
    `${BASE_URL}/api/auth/login`,
    JSON.stringify({ Login: ADMIN.Login, Senha: ADMIN.Senha }),
    { headers: JSON_HEADERS }
  );
  if (!check(login, { 'setup: login 200': (r) => r.status === 200 })) {
    fail(`login falhou: ${login.status} ${login.body}`);
  }
  const token = login.json('token');
  const h = { headers: headers(token) };

  const medicos = [];
  for (let i = 0; i < qtdMedicos; i++) {
    const r = http.post(
      `${BASE_URL}/api/medicos`,
      JSON.stringify({
        Nome: `Medico k6 ${i + 1}`,
        Email: `medico${i + 1}@example.com`,
        Telefone: '(47) 97777-7777',
        CRM: `CRM-SC ${10000 + i}`,
      }),
      h
    );
    if (r.status !== 201) fail(`setup: criar medico devolveu ${r.status} ${r.body}`);
    medicos.push(r.json('id'));
  }

  const pacientes = [];
  for (let i = 0; i < qtdPacientesIniciais; i++) {
    const r = http.post(
      `${BASE_URL}/api/pacientes`,
      JSON.stringify(novoPaciente(cpfUnico(0, i))),
      h
    );
    if (r.status !== 201) {
      fail(
        `setup: criar paciente devolveu ${r.status} ${r.body} ` +
          '(sobrou dado de uma execucao interrompida? apague o banco de carga e suba a API de novo)'
      );
    }
    pacientes.push(r.json('id'));
  }

  return { token, medicos, pacientes };
}

/** Remove a massa criada no setup, deixando o banco como estava. */
export function limpar(dados) {
  const h = { headers: headers(dados.token) };
  for (const id of dados.pacientes) http.del(`${BASE_URL}/api/pacientes/${id}`, null, h);
  for (const id of dados.medicos) http.del(`${BASE_URL}/api/medicos/${id}`, null, h);
}
