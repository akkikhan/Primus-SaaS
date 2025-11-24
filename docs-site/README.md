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

## Deployment to GitHub Pages

### Automatic Deployment (Recommended)

The documentation is automatically deployed to GitHub Pages when changes are pushed to the `main` or `AG-nine` branches:

**Live URL:** https://akkikhan.github.io/Primus-SaaS/

The GitHub Actions workflow (`.github/workflows/deploy-docs.yml`) will:
1. Build the Docusaurus site
2. Deploy to GitHub Pages automatically
3. Make it accessible worldwide

### Manual Deployment

If you want to deploy manually:

```bash
cd docs-site
GIT_USER=akkikhan npm run deploy
```

This will build and push directly to the `gh-pages` branch.

### First-Time Setup

To enable GitHub Pages for the first time:

1. Go to your repository on GitHub: https://github.com/akkikhan/Primus-SaaS
2. Navigate to **Settings** → **Pages**
3. Under "Build and deployment":
   - Source: **GitHub Actions**
4. The site will be live at: https://akkikhan.github.io/Primus-SaaS/

## Content structure

- `docs/intro.md` — landing doc
- `docs/modules/*` — SDK documentation for Identity Validator and Logging
- `src/pages/index.js` — marketing/hero page
- `src/css/custom.css` — theme tweaks
