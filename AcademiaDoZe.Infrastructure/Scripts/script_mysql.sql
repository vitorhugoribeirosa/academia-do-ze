-- ============================================================
-- SCRIPT DE CRIACAO DO BANCO DE DADOS: db_academia_do_ze (MySQL / MariaDB)
-- Baseado na camada de dominio AcademiaDoZe.Domain
-- ============================================================

CREATE TABLE IF NOT EXISTS tb_logradouro (
    id_logradouro INT AUTO_INCREMENT NOT NULL,
    cep VARCHAR(8) NOT NULL,
    nome VARCHAR(150) NOT NULL,
    bairro VARCHAR(100) NOT NULL,
    cidade VARCHAR(100) NOT NULL,
    estado CHAR(2) NOT NULL,
    pais VARCHAR(50) NOT NULL DEFAULT 'Brasil',
    PRIMARY KEY (id_logradouro),
    UNIQUE KEY uq_tb_logradouro_cep (cep),
    INDEX ix_tb_logradouro_cep (cep),
    INDEX ix_tb_logradouro_cidade (cidade)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS tb_aluno (
    id_aluno INT AUTO_INCREMENT NOT NULL,
    cpf VARCHAR(11) NOT NULL,
    nome VARCHAR(150) NOT NULL,
    nascimento DATE NOT NULL,
    telefone VARCHAR(15) NOT NULL,
    email VARCHAR(150) NOT NULL,
    logradouro_id INT NOT NULL,
    numero VARCHAR(20) NOT NULL,
    complemento VARCHAR(100) NULL,
    senha VARCHAR(255) NOT NULL,
    foto LONGBLOB NULL,
    PRIMARY KEY (id_aluno),
    UNIQUE KEY uq_tb_aluno_cpf (cpf),
    INDEX ix_tb_aluno_cpf (cpf),
    CONSTRAINT fk_tb_aluno_tb_logradouro FOREIGN KEY (logradouro_id)
        REFERENCES tb_logradouro (id_logradouro) ON DELETE RESTRICT ON UPDATE CASCADE
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS tb_colaborador (
    id_colaborador INT AUTO_INCREMENT NOT NULL,
    cpf VARCHAR(11) NOT NULL,
    nome VARCHAR(150) NOT NULL,
    nascimento DATE NOT NULL,
    telefone VARCHAR(15) NOT NULL,
    email VARCHAR(150) NOT NULL,
    logradouro_id INT NOT NULL,
    numero VARCHAR(20) NOT NULL,
    complemento VARCHAR(100) NULL,
    senha VARCHAR(255) NOT NULL,
    foto LONGBLOB NULL,
    admissao DATE NOT NULL,
    tipo INT NOT NULL,
    vinculo INT NOT NULL,
    PRIMARY KEY (id_colaborador),
    UNIQUE KEY uq_tb_colaborador_cpf (cpf),
    INDEX ix_tb_colaborador_cpf (cpf),
    CONSTRAINT fk_tb_colaborador_tb_logradouro FOREIGN KEY (logradouro_id)
        REFERENCES tb_logradouro (id_logradouro) ON DELETE RESTRICT ON UPDATE CASCADE
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS tb_matricula (
    id_matricula INT AUTO_INCREMENT NOT NULL,
    aluno_id INT NOT NULL,
    plano INT NOT NULL,
    data_inicio DATE NOT NULL,
    data_fim DATE NOT NULL,
    objetivo VARCHAR(500) NOT NULL,
    restricao_medica INT NOT NULL DEFAULT 0,
    obs_restricao VARCHAR(500) NULL,
    laudo_medico LONGBLOB NULL,
    PRIMARY KEY (id_matricula),
    INDEX ix_tb_matricula_aluno_id (aluno_id),
    INDEX ix_tb_matricula_data_fim (data_fim),
    CONSTRAINT fk_tb_matricula_tb_aluno FOREIGN KEY (aluno_id)
        REFERENCES tb_aluno (id_aluno) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS tb_acesso (
    id_acesso INT AUTO_INCREMENT NOT NULL,
    pessoa_tipo INT NOT NULL,
    pessoa_id INT NOT NULL,
    data_hora DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (id_acesso),
    INDEX ix_tb_acesso_pessoa (pessoa_tipo, pessoa_id),
    INDEX ix_tb_acesso_data_hora (data_hora)
) ENGINE=InnoDB;
