import jsPDF from 'jspdf';
import { DocumentationDto } from './documentationService';

export const generatePDF = (documentation: DocumentationDto): void => {
    const doc = new jsPDF();
    let yPos = 20;
    const pageWidth = doc.internal.pageSize.getWidth();
    const margin = 20;
    const maxWidth = pageWidth - 2 * margin;

    // Helper to check page break
    const checkPageBreak = (heightNeeded: number) => {
        if (yPos + heightNeeded > 280) {
            doc.addPage();
            yPos = 20;
        }
    };

    // Title
    doc.setFontSize(20);
    doc.text(documentation.applicationName, margin, yPos);
    yPos += 10;

    // Metadata
    doc.setFontSize(10);
    doc.text(`Client ID: ${documentation.primusClientId}`, margin, yPos);
    yPos += 6;
    doc.text(`Stack: ${documentation.stack}`, margin, yPos);
    yPos += 6;
    doc.text(`Generated: ${new Date(documentation.generatedAt).toLocaleString()}`, margin, yPos);
    yPos += 15;

    // Modules
    documentation.modules.forEach((module) => {
        checkPageBreak(40); // Header space

        doc.setFontSize(16);
        doc.setFont('helvetica', 'bold');
        doc.text(`${module.moduleName} v${module.version}`, margin, yPos);
        doc.setFont('helvetica', 'normal');
        yPos += 10;

        if (module.isBreakingChange) {
            doc.setTextColor(255, 0, 0);
            doc.setFontSize(10);
            doc.text('⚠️ Breaking Change', margin, yPos);
            doc.setTextColor(0, 0, 0);
            yPos += 8;
        }

        if (module.releaseNotes) {
            doc.setFontSize(12);
            doc.setFont('helvetica', 'bold');
            doc.text('Release Notes:', margin, yPos);
            doc.setFont('helvetica', 'normal');
            yPos += 6;

            doc.setFontSize(10);
            const notes = doc.splitTextToSize(module.releaseNotes, maxWidth);
            checkPageBreak(notes.length * 5);
            doc.text(notes, margin, yPos);
            yPos += notes.length * 5 + 5;
        }

        // Integration Steps
        doc.setFontSize(12);
        doc.setFont('helvetica', 'bold');
        doc.text('Integration Steps:', margin, yPos);
        doc.setFont('helvetica', 'normal');
        yPos += 6;

        doc.setFontSize(10);
        module.integrationSteps.forEach((step, idx) => {
            const stepText = doc.splitTextToSize(`${idx + 1}. ${step}`, maxWidth);
            checkPageBreak(stepText.length * 5);
            doc.text(stepText, margin, yPos);
            yPos += stepText.length * 5 + 2;
        });
        yPos += 5;

        // Code Snippets
        if (module.codeSnippets && Object.keys(module.codeSnippets).length > 0) {
            doc.setFontSize(12);
            doc.setFont('helvetica', 'bold');
            doc.text('Code Snippets:', margin, yPos);
            doc.setFont('helvetica', 'normal');
            yPos += 6;

            Object.entries(module.codeSnippets).forEach(([filename, code]) => {
                checkPageBreak(20);
                doc.setFontSize(10);
                doc.setFont('courier', 'bold');
                doc.text(filename, margin, yPos);
                yPos += 5;

                doc.setFont('courier', 'normal');
                doc.setFontSize(9);
                const codeLines = doc.splitTextToSize(code, maxWidth);
                checkPageBreak(codeLines.length * 4);
                doc.text(codeLines, margin, yPos);
                yPos += codeLines.length * 4 + 10;
                doc.setFont('helvetica', 'normal'); // Reset font
            });
        }

        yPos += 10;
    });

    doc.save(`${documentation.applicationName.replace(/\s+/g, '_')}_documentation.pdf`);
};

export const generateMarkdown = (documentation: DocumentationDto): void => {
    let markdown = `# ${documentation.applicationName}\n\n`;
    markdown += `**Client ID:** ${documentation.primusClientId}\n\n`;
    markdown += `**Stack:** ${documentation.stack}\n\n`;
    markdown += `**Generated:** ${new Date(documentation.generatedAt).toLocaleString()}\n\n`;
    markdown += `---\n\n`;

    documentation.modules.forEach((module) => {
        markdown += `## ${module.moduleName} v${module.version}\n\n`;

        if (module.isBreakingChange) {
            markdown += `⚠️ **Breaking Change**\n\n`;
        }

        if (module.releaseNotes) {
            markdown += `### Release Notes\n\n${module.releaseNotes}\n\n`;
        }

        markdown += `### Integration Steps\n\n`;
        module.integrationSteps.forEach((step, idx) => {
            markdown += `${idx + 1}. ${step}\n`;
        });
        markdown += `\n`;

        if (module.codeSnippets && Object.keys(module.codeSnippets).length > 0) {
            markdown += `### Code Snippets\n\n`;
            Object.entries(module.codeSnippets).forEach(([filename, code]) => {
                // Detect language based on extension or default to text
                const lang = filename.endsWith('.json') ? 'json' :
                    filename.endsWith('.ts') ? 'typescript' :
                        filename.endsWith('.cs') ? 'csharp' : 'text';

                markdown += `#### ${filename}\n\n\`\`\`${lang}\n${code}\n\`\`\`\n\n`;
            });
        }

        markdown += `---\n\n`;
    });

    const blob = new Blob([markdown], { type: 'text/markdown' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `${documentation.applicationName.replace(/\s+/g, '_')}_documentation.md`;
    a.click();
    URL.revokeObjectURL(url);
};

export const generateJSON = (documentation: DocumentationDto): void => {
    const jsonString = JSON.stringify(documentation, null, 2);
    const blob = new Blob([jsonString], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `${documentation.applicationName.replace(/\s+/g, '_')}_documentation.json`;
    a.click();
    URL.revokeObjectURL(url);
};
