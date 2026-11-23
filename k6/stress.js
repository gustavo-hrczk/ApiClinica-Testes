// STRESS TEST
// Objetivo: observar a estabilidade e a degradacao da API acima da carga normal.
//
// Carga:    sobe em degraus ate 100 usuarios virtuais — 5 vezes a carga media
//           (20 VUs). O degrau intermediario de 50 VUs (2,5x) permite ver em que
//           ponto o tempo de resposta comeca a piorar.
// Duracao:  6 min — 1 min subindo ate 50, 1 min em 50, 1 min subindo ate 100,
//           2 min em 100 e 1 min de descida (para ver se a API se recupera).
// Cenario:  o mesmo do average load, para os resultados serem comparaveis.
// Aceite:   sob estresse aceitamos degradacao, mas nao colapso: menos de 5% de
//           falhas, mais de 95% das verificacoes corretas e p95 abaixo de 2 s.
import { preparar, limpar } from './lib/api.js';
import { jornadaRecepcao } from './lib/cenarios.js';

const VUS = 100;

export const options = {
  stages: [
    { duration: '1m', target: VUS / 2 },
    { duration: '1m', target: VUS / 2 },
    { duration: '1m', target: VUS },
    { duration: '2m', target: VUS },
    { duration: '1m', target: 0 },
  ],
  thresholds: {
    http_req_failed: ['rate<0.05'],
    checks: ['rate>0.95'],
    http_req_duration: ['p(95)<2000'],
    'http_req_duration{tipo:leitura}': ['p(95)<2000'],
    'http_req_duration{tipo:escrita}': ['p(95)<2000'],
  },
};

export function setup() {
  return preparar(VUS, 200);
}

export default function (dados) {
  jornadaRecepcao(dados);
}

export function teardown(dados) {
  limpar(dados);
}
