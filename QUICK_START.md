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

## ⚠️ Important: Repository URL Configuration

The current gh-pages branch is configured for:
- **Current baseUrl**: `/Primus-SaaS-Framework/`
- **Expected baseUrl**: `/Primus-SaaS/` (for akkikhan/Primus-SaaS repository)

### What This Means

If you deploy the site as-is, you may encounter:
- Broken links and navigation
- Missing CSS and JavaScript files
- 404 errors when clicking on documentation pages

### How to Fix

**Option 1: Test First (Recommended)**
1. Deploy the site using the steps above
2. Visit `https://akkikhan.github.io/Primus-SaaS/`
3. If links work correctly, no action needed
4. If you see routing issues, proceed with Option 2

**Option 2: Update baseUrl Before Deploying**
If you have access to the Docusaurus source:
1. Find `docusaurus.config.js` (in the source, not gh-pages branch)
2. Update the baseUrl:
   ```js
   module.exports = {
     baseUrl: '/Primus-SaaS/',  // Changed from /Primus-SaaS-Framework/
     // ... other config
   };
   ```
3. Rebuild the site: `npm run build` or `yarn build`
4. Update the gh-pages branch with the new build
5. Run the deployment workflow

**Option 3: Use Custom Domain**
Configure a custom domain (e.g., docs.yourdomain.com) to avoid baseUrl issues entirely. See `GITHUB_PAGES_SETUP.md` for details.
