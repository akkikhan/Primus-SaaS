# GitHub Actions Workflows

## deploy-docs.yml

Automatically deploys the Docusaurus documentation from the `gh-pages` branch to GitHub Pages.

### How it works

1. Checks out the `gh-pages` branch
2. Uploads the content as a Pages artifact
3. Deploys to GitHub Pages

### Manual Trigger

To manually deploy the documentation:

1. Go to the **Actions** tab in GitHub
2. Select **Deploy Documentation to GitHub Pages**
3. Click **Run workflow**
4. Click the **Run workflow** button

### Automatic Trigger

The workflow automatically runs when:
- Changes are pushed to the `gh-pages` branch
- This ensures the deployed site stays in sync with the gh-pages branch

### Requirements

- GitHub Pages must be enabled in repository settings with source set to "GitHub Actions"
- The `gh-pages` branch must exist with the built Docusaurus site

### Troubleshooting

See [GITHUB_PAGES_SETUP.md](../GITHUB_PAGES_SETUP.md) for detailed setup instructions and troubleshooting.
