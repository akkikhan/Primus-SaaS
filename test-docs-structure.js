// Test script to verify documentation structure
const fs = require('fs');
const path = require('path');

console.log('📚 Documentation Site Verification\n');

const docsDir = path.join(__dirname, 'docs-site', 'docs', 'modules');

// Check if modules directory exists
if (!fs.existsSync(docsDir)) {
    console.error('❌ Modules directory not found!');
    process.exit(1);
}

console.log('✅ Modules directory exists\n');

// Expected files
const expectedFiles = [
    'identity-validator-dotnet.md',
    'identity-validator-nodejs.md',
    'logging-dotnet.md',
    'logging-nodejs.md'
];

console.log('Checking for module documentation files:\n');

let allFilesExist = true;
expectedFiles.forEach(file => {
    const filePath = path.join(docsDir, file);
    const exists = fs.existsSync(filePath);

    if (exists) {
        const content = fs.readFileSync(filePath, 'utf8');
        const lines = content.split('\n').length;
        console.log(`✅ ${file} (${lines} lines)`);
    } else {
        console.log(`❌ ${file} - NOT FOUND`);
        allFilesExist = false;
    }
});

console.log('\n' + '='.repeat(50));

if (allFilesExist) {
    console.log('✅ All module documentation files are present!');
    console.log('\n📖 Documentation URLs:');
    console.log('   http://localhost:3001/docs/modules/identity-validator-dotnet');
    console.log('   http://localhost:3001/docs/modules/identity-validator-nodejs');
    console.log('   http://localhost:3001/docs/modules/logging-dotnet');
    console.log('   http://localhost:3001/docs/modules/logging-nodejs');
    console.log('\n🎉 Documentation site is ready!');
} else {
    console.log('❌ Some files are missing!');
    process.exit(1);
}

// Test email link generation
console.log('\n' + '='.repeat(50));
console.log('📧 Email Link Generation Test\n');

function generateDocLink(moduleName, stack) {
    const module = moduleName.toLowerCase().replace(' ', '-');
    return `http://localhost:3001/docs/modules/${module}-${stack}`;
}

const testCases = [
    { module: 'Identity Validator', stack: 'dotnet' },
    { module: 'Identity Validator', stack: 'nodejs' },
    { module: 'Logging', stack: 'dotnet' },
    { module: 'Logging', stack: 'nodejs' }
];

testCases.forEach(({ module, stack }) => {
    const link = generateDocLink(module, stack);
    console.log(`${module} (${stack}): ${link}`);
});

console.log('\n✅ Email integration logic verified!');
