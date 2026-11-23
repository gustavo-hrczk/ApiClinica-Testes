// SMOKE TEST
// Objetivo: confirmar que os scripts funcionam e que a API responde corretamente
// antes de gastar tempo com os testes longos. Nao mede capacidade.
//
// Carga:    1 usuario virtual, 30 segundos.
// Cenario:  jornada completa da recepcao (CRUD de paciente e consulta) mais as
//           verificacoes de erro (401, 404, 400 e 409).
// Aceite:   nenhuma requisicao pode falhar e todas as verificacoes devem passar.
//           Com 1 usuario nao ha fila, entao o p95 precisa ficar bem abaixo do
//           limite usado sob carga (300 ms).
import { sleep } from 'k6';
import { preparar, limpar } from './lib/api.js';
import { jornadaRecepcao, verificacoesDeErro } from './lib/cenarios.js';

const VUS = 1;

export const options = {
  vus: VUS,
  duration: '30s',
  thresholds: {
    http_req_failed: ['rate==0'],
    checks: ['rate==1'],
    http_req_duration: ['p(95)<300'],
  },
};

export function setup() {
  return preparar(VUS, 20);
}

export default function (dados) {
  jornadaRecepcao(dados);
  verificacoesDeErro(dados);
  sleep(1);
}

export function teardown(dados) {
  limpar(dados);
}
