import fs from 'node:fs/promises';
import {createRequire} from 'node:module';
const require=createRequire('C:/Program Files/WindowsApps/OpenAI.CodexPrimaryRuntime.v26-1119-907-0_26.1119.907.0_x64__3k8sg7r9htsxt/dependencies/node/package.json');
const {FileBlob,SpreadsheetFile}=require('@oai/artifact-tool');
const path='C:/Users/phill/OneDrive/Documents/EcoSphere/outputs/element-matrix/Element Combinations.xlsx';
const wb=await SpreadsheetFile.importXlsx(await FileBlob.load(path));
const s=wb.worksheets.getItem('Elements');
const elements=s.getRange('A2:A51').values.flat();
if(JSON.stringify(elements)!==JSON.stringify(s.getRange('B1:AY1').values[0])) throw Error('Headers differ');
const before=await wb.render({sheetName:'Elements',range:'A1:H10',scale:1,format:'png'});
await fs.writeFile(new URL('./before.png',import.meta.url),new Uint8Array(await before.arrayBuffer()));
const recipes=[
 ['Void','Fire','Sunlight'],
 ['Fire','Fire','Energy'],
 ['Ice','Fire','Water'],
 ['Water','Energy','Air'],
 ['Water','Fire','Steam'],
 ['Steam','Air','Cloud'],
 ['Cloud','Water','Rain'],
 ['Rain','Ice','Snow'],
 ['Air','Energy','Wind'],
 ['Cloud','Energy','Lightning'],
 ['Stone','Fire','Lava'],
 ['Stone','Wind','Sand'],
 ['Sand','Wind','Dust'],
 ['Stone','Rain','Soil'],
 ['Soil','Water','Clay'],
 ['Water','Sunlight','Salt'],
 ['Stone','Energy','Metal'],
 ['Stone','Salt','Crystal'],
 ['Sand','Fire','Glass'],
 ['Wood','Stone','Coal'],
 ['Algae','Stone','Oil'],
 ['Tree','Human','Wood'],
 ['Stone','Stone','Gravity'],
 ['Water','Lightning','Life'],
 ['Life','Soil','Plant'],
 ['Plant','Water','Algae'],
 ['Plant','Stone','Moss'],
 ['Life','Wood','Fungus'],
 ['Plant','Sunlight','Flower'],
 ['Plant','Wood','Tree'],
 ['Life','Water','Bacteria'],
 ['Life','Flower','Insect'],
 ['Life','Ocean','Fish'],
 ['Life','Air','Bird'],
 ['Life','Sand','Reptile'],
 ['Life','Snow','Mammal'],
 ['Mammal','Technology','Human'],
 ['Water','Water','Ocean'],
 ['Soil','Ocean','Swamp'],
 ['Life','Void','Magic'],
 ['Metal','Energy','Technology'],
 ['Air','Wind','Sound'],
 ['Human','Stone','City'],
 ['Wood','Fire','Ash'],
 ['Fire','Air','Smoke'],
 ['Fungus','Bacteria','Poison'],
 // A few alternative routes; the remainder create optional terminal elements.
 ['Water','Void','Ice'],
 ['Plant','Plant','Tree'],
 ['Coal','Fire','Energy'],
 ['Void','Void','Space'],
 ['Life','Magic','Alien'],
 ['Sand','Plant','Cactus'],
 ['Plant','Swamp','Fern'],
 ['Tree','Tree','Forest'],
 ['Flower','Insect','Honey'],
 ['Flower','Wind','Pollen'],
 ['Bird','Water','Duck'],
 ['Fish','Poison','Jellyfish'],
 ['Reptile','Swamp','Crocodile'],
 ['Reptile','Magic','Dragon'],
 ['Human','Magic','Wizard'],
 ['Human','Metal','Tool'],
 ['Human','Clay','Pottery'],
 ['Clay','Fire','Brick'],
 ['Sand','Glass','Hourglass'],
 ['Metal','Fire','Forge'],
 ['Metal','Lightning','Electromagnet'],
 ['Metal','Water','Rust'],
 ['Metal','Sound','Bell'],
 ['Wood','Water','Boat'],
 ['Wood','Wind','Windmill'],
 ['Wood','Sound','Drum'],
 ['Oil','Fire','Explosion'],
 ['Poison','Human','Disease'],
 ['Bacteria','Human','Infection'],
 ['Human','Plant','Agriculture'],
 ['Human','Flower','Garden'],
 ['Human','Fish','Fishing'],
 ['Human','Ice','Igloo'],
 ['Technology','Air','Airplane'],
 ['Technology','Water','Submarine'],
 ['Technology','Sunlight','Solar Panel'],
 ['Technology','Sound','Radio'],
 ['Technology','Life','Robot'],
 ['Magic','Crystal','Magic Wand'],
 ['Gravity','Sunlight','Black Hole'],
 ['Gravity','Ocean','Tide'],
 ['Wind','Ocean','Wave'],
 ['Lava','Ocean','Island'],
 ['Lava','Water','Obsidian'],
 ['Snow','Wind','Blizzard'],
 ['Rain','Sunlight','Rainbow'],
 ['Cloud','Wind','Storm'],
 ['Dust','Wind','Dust Storm'],
 ['Salt','Ice','Brine'],
 ['City','Smoke','Smog'],
 ['City','Water','Canal'],
 ['City','Plant','Park'],
 ['City','Magic','Enchanted City'],
];
// Keep all ingredients on the original axes; terminal discoveries have no axes.
const valid=recipes.filter(([a,b])=>elements.includes(a)&&elements.includes(b));
const grid=Array.from({length:50},()=>Array(50).fill('NA'));
const seen=new Set();
for(const [a,b,result] of valid){
 const i=elements.indexOf(a),j=elements.indexOf(b), key=[i,j].sort((x,y)=>x-y).join(':');
 if(seen.has(key))throw Error(`Duplicate pair ${a} + ${b}`);
 seen.add(key);grid[i][j]=result;grid[j][i]=result;
}
const reachable=new Set(['Void','Fire','Stone','Ice']);
let changed=true;
while(changed){changed=false;for(const [a,b,r] of valid)if(reachable.has(a)&&reachable.has(b)&&!reachable.has(r)){reachable.add(r);changed=true;}}
const missing=elements.filter(e=>!reachable.has(e));
if(missing.length)throw Error(`Unreachable: ${missing.join(', ')}`);
for(let i=0;i<50;i++)for(let j=0;j<50;j++)if(grid[i][j]!==grid[j][i])throw Error('Asymmetry');
s.getRange('B2:AY51').values=grid;
wb.recalculate();
const after=await wb.render({sheetName:'Elements',range:'A1:H12',scale:1,format:'png'});
await fs.writeFile(new URL('./filled-preview.png',import.meta.url),new Uint8Array(await after.arrayBuffer()));
const out=await SpreadsheetFile.exportXlsx(wb);await out.save(path);
console.log(JSON.stringify({recipes:valid.length,terminalResults:[...new Set(valid.map(x=>x[2]))].filter(x=>!elements.includes(x)),missing,unproducible:elements.filter(e=>!valid.some(x=>x[2]===e)),NA:grid.flat().filter(x=>x==='NA').length}));
