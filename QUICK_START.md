# Quick Start: Publish Documentation to GitHub Pages

## ✅ Completed (Automated)
- GitHub Actions workflow created
- Documentation for full setup process available in `GITHUB_PAGES_SETUP.md`

## ⏳ Next Steps (Manual - Required)

You need to complete these manual steps in GitHub's web interface:

### Step 1: Enable GitHub Pages

1. Go to https://github.com/akkikhan/Primus-SaaS/settings/pages
2. Under "Build and deployment":
   - **Source**: Select `GitHub Actions` (recommended)
   - OR select `Deploy from a branch` and choose `gh-pages` branch

### Step 2: Deploy the Site

**Option A - Using GitHub Actions (Recommended):**
1. Go to https://github.com/akkikhan/Primus-SaaS/actions
2. Click on "Deploy Documentation to GitHub Pages" workflow
3. Click "Run workflow" → "Run workflow"
4. Wait for the workflow to complete

**Option B - Direct from gh-pages branch:**
- If you selected "Deploy from a branch" in Step 1, GitHub will automatically deploy
- No additional action needed

### Step 3: Verify

After a few minutes, your documentation will be live at:
- `https://akkikhan.github.io/Primus-SaaS/`

## 🔧 Troubleshooting

If the workflow fails with permissions error:
1. Go to https://github.com/akkikhan/Primus-SaaS/settings/actions
2. Scroll to "Workflow permissions"
3. Select "Read and write permissions"
4. Click "Save"
5. Re-run the workflow

## 📚 Full Documentation

See `GITHUB_PAGES_SETUP.md` for:
- Detailed setup instructions
- Configuration options
- Custom domain setup
- Full troubleshooting guide
- How to update documentation

## ⚠️ Note about Repository URL

The current gh-pages branch is configured for:
- `baseUrl: /Primus-SaaS-Framework/`

For this repository (akkikhan/Primus-SaaS), the baseUrl should be:
- `baseUrl: /Primus-SaaS/`

If you encounter routing issues after deployment, you may need to update the Docusaurus configuration and rebuild the site.
