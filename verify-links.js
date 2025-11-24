const fs = require('fs');
const path = require('path');

console.log('🔍 Verifying Documentation Links...\n');

const docsDir = path.join(__dirname, 'docs-site', 'docs', 'modules');
const files = fs.readdirSync(docsDir);

let errorCount = 0;

files.forEach(file => {
    if (!file.endsWith('.md')) return;

    const content = fs.readFileSync(path.join(docsDir, file), 'utf8');
    const links = content.match(/\[.*?\]\((.*?)\)/g);

    if (links) {
        links.forEach(link => {
            const match = link.match(/\[.*?\]\((.*?)\)/);
            if (match) {
                let url = match[1];
                // Ignore external links
                if (url.startsWith('http')) return;

                // Normalize relative links
                if (url.startsWith('./')) {
                    url = url.substring(2);
                }

                // Check if file exists
                const targetFile = path.join(docsDir, url + '.md');
                if (!fs.existsSync(targetFile)) {
                    console.error(`❌ BROKEN LINK in ${file}: ${url}`);
                    console.error(`   Expected: ${targetFile}`);
                    errorCount++;
                } else {
                    console.log(`✅ Valid link in ${file}: ${url}`);
                }
            }
        });
    }
});

console.log('\n' + '='.repeat(50));
if (errorCount === 0) {
    console.log('🎉 All internal links are VALID!');
} else {
    console.error(`❌ Found ${errorCount} broken links!`);
    process.exit(1);
}
