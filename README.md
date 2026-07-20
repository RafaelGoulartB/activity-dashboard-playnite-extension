# Activity Dashboard

Extensão para Playnite 6 que adiciona à sidebar um painel de atividade da biblioteca.

## Recursos

- Heatmap dos últimos 52 semanas, calculado a partir de sessões rastreadas.
- Tempo total, jogos jogados, inicializações e atividade dos últimos 30 dias do Playnite.
- Rankings de jogos, plataformas e gêneros.
- Lista das sessões recentes e opção para apagar somente o histórico da extensão.

## Histórico de sessões

O Playnite oferece os totais e a última atividade dos jogos, mas não expõe o histórico completo de sessões. Por isso, o heatmap começa a registrar dados quando a extensão é instalada. Os cartões e rankings continuam usando o histórico total já disponível na biblioteca.

## Desenvolvimento

```powershell
dotnet build src/Dashboard.csproj
dotnet test tests/Dashboard.Tests/Dashboard.Tests.csproj
```

Copie `Dashboard.dll` e suas dependências, junto de `extension.yaml`, para a pasta de extensões do Playnite.
