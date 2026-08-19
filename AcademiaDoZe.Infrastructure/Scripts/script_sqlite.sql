-- ============================================================
-- SCRIPT DE CRIACAO DO BANCO DE DADOS: db_academia_do_ze (SQLite)
-- Baseado na camada de dominio AcademiaDoZe.Domain
-- ============================================================

PRAGMA foreign_keys = ON;

CREATE TABLE IF NOT EXISTS tb_logradouro (
    id_logradouro INTEGER PRIMARY KEY AUTOINCREMENT,
    cep TEXT NOT NULL UNIQUE,
    nome TEXT NOT NULL,
    bairro TEXT NOT NULL,
    cidade TEXT NOT NULL,
    estado TEXT NOT NULL,
    pais TEXT NOT NULL DEFAULT 'Brasil'
);

CREATE INDEX IF NOT EXISTS ix_tb_logradouro_cep ON tb_logradouro(cep);
CREATE INDEX IF NOT EXISTS ix_tb_logradouro_cidade ON tb_logradouro(cidade);

CREATE TABLE IF NOT EXISTS tb_aluno (
    id_aluno INTEGER PRIMARY KEY AUTOINCREMENT,
    cpf TEXT NOT NULL UNIQUE,
    nome TEXT NOT NULL,
    nascimento TEXT NOT NULL,
    telefone TEXT NOT NULL,
    email TEXT NOT NULL,
    logradouro_id INTEGER NOT NULL,
    numero TEXT NOT NULL,
    complemento TEXT NULL,
    senha TEXT NOT NULL,
    foto BLOB NULL,
    FOREIGN KEY (logradouro_id) REFERENCES tb_logradouro(id_logradouro) ON DELETE RESTRICT ON UPDATE CASCADE
);

CREATE INDEX IF NOT EXISTS ix_tb_aluno_cpf ON tb_aluno(cpf);

CREATE TABLE IF NOT EXISTS tb_colaborador (
    id_colaborador INTEGER PRIMARY KEY AUTOINCREMENT,
    cpf TEXT NOT NULL UNIQUE,
    nome TEXT NOT NULL,
    nascimento TEXT NOT NULL,
    telefone TEXT NOT NULL,
    email TEXT NOT NULL,
    logradouro_id INTEGER NOT NULL,
    numero TEXT NOT NULL,
    complemento TEXT NULL,
    senha TEXT NOT NULL,
    foto BLOB NULL,
    admissao TEXT NOT NULL,
    tipo INTEGER NOT NULL,
    vinculo INTEGER NOT NULL,
    FOREIGN KEY (logradouro_id) REFERENCES tb_logradouro(id_logradouro) ON DELETE RESTRICT ON UPDATE CASCADE
);

CREATE INDEX IF NOT EXISTS ix_tb_colaborador_cpf ON tb_colaborador(cpf);

CREATE TABLE IF NOT EXISTS tb_matricula (
    id_matricula INTEGER PRIMARY KEY AUTOINCREMENT,
    aluno_id INTEGER NOT NULL,
    plano INTEGER NOT NULL,
    data_inicio TEXT NOT NULL,
    data_fim TEXT NOT NULL,
    objetivo TEXT NOT NULL,
    restricao_medica INTEGER NOT NULL DEFAULT 0,
    obs_restricao TEXT NULL,
    laudo_medico BLOB NULL,
    FOREIGN KEY (aluno_id) REFERENCES tb_aluno(id_aluno) ON DELETE CASCADE ON UPDATE CASCADE
);

CREATE INDEX IF NOT EXISTS ix_tb_matricula_aluno_id ON tb_matricula(aluno_id);
CREATE INDEX IF NOT EXISTS ix_tb_matricula_data_fim ON tb_matricula(data_fim);

CREATE TABLE IF NOT EXISTS tb_acesso (
    id_acesso INTEGER PRIMARY KEY AUTOINCREMENT,
    pessoa_tipo INTEGER NOT NULL,
    pessoa_id INTEGER NOT NULL,
    data_hora TEXT NOT NULL DEFAULT (CURRENT_TIMESTAMP)
);

CREATE INDEX IF NOT EXISTS ix_tb_acesso_pessoa ON tb_acesso(pessoa_tipo, pessoa_id);
CREATE INDEX IF NOT EXISTS ix_tb_acesso_data_hora ON tb_acesso(data_hora);
