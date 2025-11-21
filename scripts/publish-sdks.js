const { execSync } = require('child_process');
const path = require('path');
const fs = require('fs');

const rootDir = path.resolve(__dirname, '..');
const nodeSdkDir = path.join(rootDir, 'sdk', 'nodejs', 'primus-identity-validator');
const dotnetSdkDir = path.join(rootDir, 'sdk', 'dotnet', 'PrimusSaaS.Identity.Validator');

function runCommand(command, cwd) {
    console.log(`\n> Running: ${command} in ${cwd}`);
    try {
        execSync(command, { cwd, stdio: 'inherit' });
        console.log('✅ Command successful');
    } catch (error) {
        console.error('❌ Command failed');
        process.exit(1);
    }
}

console.log('📦 STARTING SDK PUBLISH VERIFICATION 📦');
console.log('=======================================');

// 1. Node.js SDK
console.log('\n👉 Verifying Node.js SDK...');
if (fs.existsSync(nodeSdkDir)) {
    // npm install first to ensure deps
    runCommand('npm install', nodeSdkDir);
    // npm pack to create tarball (simulates publish)
    runCommand('npm pack', nodeSdkDir);
    console.log('✅ Node.js SDK packed successfully.');
} else {
    console.error(`❌ Node.js SDK directory not found: ${nodeSdkDir}`);
}

// 2. .NET SDK
console.log('\n👉 Verifying .NET SDK...');
if (fs.existsSync(dotnetSdkDir)) {
    // dotnet pack
    runCommand('dotnet pack -c Release', dotnetSdkDir);
    console.log('✅ .NET SDK packed successfully.');
} else {
    console.error(`❌ .NET SDK directory not found: ${dotnetSdkDir}`);
}

console.log('\n=======================================');
console.log('🎉 SDK PUBLISH VERIFICATION COMPLETE');
console.log('To actually publish, you would run:');
console.log('  npm publish (in nodejs dir)');
console.log('  dotnet nuget push bin/Release/*.nupkg ... (in dotnet dir)');
