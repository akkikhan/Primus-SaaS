# GitHub Pages Setup Guide

This guide explains how to enable and deploy your Docusaurus documentation to GitHub Pages.

## Current Setup

- ✅ Your repository has a `gh-pages` branch with built Docusaurus site
- ✅ GitHub Actions workflow created (`.github/workflows/deploy-docs.yml`)
- ⏳ GitHub Pages needs to be enabled in repository settings

## Required Steps to Enable GitHub Pages

### Option 1: Deploy from GitHub Actions (Recommended)

This is the modern approach that uses GitHub Actions for deployment.

1. **Go to Repository Settings**
   - Navigate to your repository on GitHub
   - Click on **Settings** tab
   - Click on **Pages** in the left sidebar

2. **Configure Source**
   - Under "Build and deployment"
   - Set **Source** to: `GitHub Actions`
   - (NOT "Deploy from a branch")

3. **Trigger Deployment**
   - Option A: Push any change to trigger the workflow
   - Option B: Go to **Actions** tab → **Deploy Documentation to GitHub Pages** → **Run workflow** → **Run workflow** button

4. **Verify Deployment**
   - After workflow completes, your site will be available at:
   - `https://akkikhan.github.io/Primus-SaaS/`

### Option 2: Deploy from gh-pages Branch (Legacy)

This option serves the existing `gh-pages` branch directly without GitHub Actions.

1. **Go to Repository Settings**
   - Navigate to your repository on GitHub
   - Click on **Settings** tab
   - Click on **Pages** in the left sidebar

2. **Configure Source**
   - Under "Build and deployment"
   - Set **Source** to: `Deploy from a branch`
   - Set **Branch** to: `gh-pages` and folder to `/ (root)`
   - Click **Save**

3. **Verify Deployment**
   - Your site will be available at:
   - `https://akkikhan.github.io/Primus-SaaS/`
   - Note: The site may take a few minutes to deploy

## Workflow Details

The GitHub Actions workflow (`.github/workflows/deploy-docs.yml`):

- **Triggers**: 
  - Automatically on push to `gh-pages` branch
  - Manually via "Run workflow" button in Actions tab
  
- **What it does**:
  1. Checks out the `gh-pages` branch
  2. Configures GitHub Pages
  3. Uploads the site as an artifact
  4. Deploys to GitHub Pages

## Updating Documentation

### If Documentation Source Exists

If you have Docusaurus source files (with `docusaurus.config.js`):

1. Make changes to your documentation source
2. Build the site: `npm run build` or `yarn build`
3. Deploy to gh-pages branch
4. The workflow will automatically deploy to GitHub Pages

### If Only Built Site Exists

If you only have the built site in the `gh-pages` branch:

1. Update the `gh-pages` branch with new content
2. Push the changes
3. The workflow will automatically deploy the updated site

## Troubleshooting

### Site Not Loading

- Check that baseUrl in Docusaurus config matches your repository name
- Current baseUrl: `/Primus-SaaS-Framework/`
- Should be: `/Primus-SaaS/` for `akkikhan/Primus-SaaS` repo

### Workflow Permissions Error

If you see "Resource not accessible by integration":

1. Go to **Settings** → **Actions** → **General**
2. Scroll to "Workflow permissions"
3. Select "Read and write permissions"
4. Check "Allow GitHub Actions to create and approve pull requests"
5. Click **Save**

### 404 Errors

- Verify the GitHub Pages source is set correctly
- Check that the `gh-pages` branch exists and has content
- Ensure baseUrl matches your repository structure

## Custom Domain (Optional)

To use a custom domain like `docs.yourdomain.com`:

1. Add a `CNAME` file to your `gh-pages` branch with your domain
2. Configure DNS with your domain provider:
   - Add a CNAME record pointing to `akkikhan.github.io`
3. Go to repository **Settings** → **Pages**
4. Enter your custom domain and click **Save**
5. Enable "Enforce HTTPS" (recommended)

## Notes

- The workflow uses GitHub Actions' built-in deployment system
- No personal access tokens or deploy keys needed
- The `gh-pages` branch is preserved and used as the deployment source
- If you have a Docusaurus source elsewhere, you can modify the workflow to build from source

## Next Steps

1. Enable GitHub Pages in repository settings (choose Option 1 or 2 above)
2. Run the workflow manually or push a change to trigger it
3. Verify your documentation site is live
4. (Optional) Update baseUrl if needed for correct routing
