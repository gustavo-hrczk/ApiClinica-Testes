// AVERAGE LOAD TEST
// Objetivo: avaliar o desempenho da API em um dia normal de uso.
//
// Carga:    20 usuarios virtuais simultaneos. Premissa: uma rede de clinicas com
//           cerca de 20 recepcionistas usando o sistema ao mesmo tempo no horario
//           de pico. Cada uma faz uma jornada (11 requisicoes) a cada ~4 s, o que
//           da em torno de 55 requisicoes por segundo.
// Duracao:  5 min — 1 min de subida gradual, 3 min de carga constante (tempo
//           suficiente para o p95 estabilizar) e 1 min de descida.
// Cenario:  jornada completa da recepcao: listagens, cadastro e atualizacao de
//           paciente, agendamento e reagendamento de consulta, e exclusoes.
// Aceite:   menos de 1% de falhas, mais de 99% das verificacoes corretas e
//           p95 abaixo de 500 ms (leituras abaixo de 400 ms, escritas abaixo de
//           600 ms) — limites em que o uso ainda parece instantaneo na tela.
import { preparar, limpar } from './lib/api.js';
import { jornadaRecepcao } from './lib/cenarios.js';

const VUS = 20;

export const options = {
  stages: [
    { duration: '1m', target: VUS },
    { duration: '3m', target: VUS },
    { duration: '1m', target: 0 },
  ],
  thresholds: {
    http_req_failed: ['rate<0.01'],
    checks: ['rate>0.99'],
    http_req_duration: ['p(95)<500'],
    'http_req_duration{tipo:leitura}': ['p(95)<400'],
    'http_req_duration{tipo:escrita}': ['p(95)<600'],
  },
};

export function setup() {
  // 200 pacientes de massa inicial: as listagens nao tem paginacao, entao o
  // volume do banco influencia diretamente o tempo de resposta.
  return preparar(VUS, 200);
}

export default function (dados) {
  jornadaRecepcao(dados);
}

export function teardown(dados) {
  limpar(dados);
}
