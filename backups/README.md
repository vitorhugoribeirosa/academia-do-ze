# Backup completo do banco de dados

Esta branch existe somente para preservar o banco anterior a qualquer reducao de dados.

Arquivo preservado:

- `db_academia_do_ze_completo_307.db`

Contagens do backup:

- 307 logradouros
- 152 alunos
- 50 colaboradores
- 89 matriculas
- 0 acessos

Verificacoes realizadas antes do envio:

- integridade SQLite: `ok`
- copia identica ao backup local validado

O aplicativo nao utiliza este arquivo. O banco ativo permanece na raiz da branch
`desenvolvimento`. Portanto, o ZIP baixado a partir de `desenvolvimento` nao inclui
esta pasta de backup.
