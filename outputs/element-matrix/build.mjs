import fs from 'node:fs/promises';
import {createRequire} from 'node:module';
const require=createRequire('C:/Program Files/WindowsApps/OpenAI.CodexPrimaryRuntime.v26-1119-907-0_26.1119.907.0_x64__3k8sg7r9htsxt/dependencies/node/package.json');
const {Workbook, SpreadsheetFile}=require('@oai/artifact-tool');
const elements = ['Void','Fire','Sunlight','Water','Air','Stone','Soil','Sand','Ice','Life','Steam','Cloud','Rain','Snow','Wind','Lightning','Lava','Smoke','Ash','Dust','Poison','Clay','Salt','Metal','Crystal','Glass','Coal','Oil','Wood','Gravity','Plant','Algae','Moss','Fungus','Flower','Tree','Bacteria','Insect','Fish','Bird','Reptile','Mammal','Human','Ocean','Swamp','Energy','Magic','Technology','Sound','City'];
if(elements.length!==50 || new Set(elements).size!==50) throw Error('Invalid element list');
const wb=Workbook.create();
const sheet=wb.worksheets.add('Elements');
sheet.getRange('A2:A51').values=elements.map(e=>[e]);
sheet.getRange('B1:AY1').values=[elements];
sheet.getRange('A1:AY51').format.font={name:'Arial',size:11};
sheet.getRange('A1:AY51').format.columnWidth=15;
sheet.getRange('A1:AY51').format.rowHeight=22;
for(const address of ['A2:A51','B1:AY1']) {
 sheet.getRange(address).format.fill='#294A3D';
 sheet.getRange(address).format.font={name:'Arial',size:11,bold:true,color:'#FFFFFF'};
}
sheet.getRange('B1:AY1').format.horizontalAlignment='center';
sheet.freezePanes.freezeRows(1);
sheet.freezePanes.freezeColumns(1);
wb.recalculate();
const down=sheet.getRange('A2:A51').values.flat();
const across=sheet.getRange('B1:AY1').values[0];
if(JSON.stringify(down)!==JSON.stringify(elements)||JSON.stringify(across)!==JSON.stringify(elements)) throw Error('Header mismatch');
const preview=await wb.render({sheetName:'Elements',range:'A1:H12',scale:1,format:'png'});
await fs.writeFile(new URL('./preview.png',import.meta.url),new Uint8Array(await preview.arrayBuffer()));
const file=await SpreadsheetFile.exportXlsx(wb);
await file.save('C:/Users/phill/OneDrive/Documents/EcoSphere/outputs/element-matrix/Element Combinations.xlsx');
console.log('Verified 50 unique elements in A2:A51 and B1:AY1.');


