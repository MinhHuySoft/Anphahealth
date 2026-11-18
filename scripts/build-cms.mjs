import { readFile, readdir, writeFile } from 'node:fs/promises';
import { build } from 'esbuild';

const project = 'src/AnphaHealth.Web';
const publicDirectory = `${project}/wwwroot/admin`;
const result = await build({
    entryPoints: [`${project}/Client/tiptap.js`],
    outfile: `${publicDirectory}/tiptap.js`,
    bundle: true,
    format: 'esm',
    target: 'es2022',
    minify: true,
    legalComments: 'linked',
    metafile: true,
    logLevel: 'info',
});

const dependencyFolders = new Set();
for (const input of Object.keys(result.metafile.inputs)) {
    const packageFolder = input.match(/^(.*node_modules\/(?:@[^/]+\/)?[^/]+)\//)?.[1];
    if (packageFolder) {
        dependencyFolders.add(packageFolder);
    }
}

const licenses = new Map();
for (const directory of [...dependencyFolders].sort()) {
    const metadata = JSON.parse(await readFile(`${directory}/package.json`, 'utf8'));
    const files = await readdir(directory);
    const licenseFilename = files.find(file => /^(license|licence)(\.(md|txt))?$/i.test(file));
    const licenseText = licenseFilename
        ? (await readFile(`${directory}/${licenseFilename}`, 'utf8')).trim()
        : `License declared by package: ${metadata.license}`;

    const packages = licenses.get(licenseText) ?? [];
    packages.push(`${metadata.name} ${metadata.version} (${metadata.license})`);
    licenses.set(licenseText, packages);
}

const notices = [...licenses].map(([license, packages]) => `${packages.join('\n')}\n\n${license}`);
await writeFile(`${publicDirectory}/tiptap-licenses.txt`, `${notices.join('\n\n---\n\n')}\n`);
