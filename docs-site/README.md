# Primus Integration Docs (Docusaurus)

Public-facing docs for integrating Primus across Node.js and .NET 8. Built with Docusaurus so the team can ship a shareable docs microsite or export content for customer emails.

## Quick start

```bash
cd docs-site
npm install
npm start   # http://localhost:3000
```

## Production build

```bash
npm run build
npm run serve   # serves the static build from ./build
```

## Content structure

- `docs/intro.md` — landing doc
- `docs/integrations/*` — stack-specific guides
- `src/pages/index.js` — marketing/hero page
- `src/css/custom.css` — theme tweaks
