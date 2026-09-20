// Export the same Settings source used by the game; fail closed on unsupported expressions.
const fs=require('fs'),path=require('path');
module.exports=function settingsCatalog(root){
 const text=fs.readFileSync(path.join(root,'src/Settings.cs'),'utf8'),en={},ko={};
 const prefix='GizmoXYZ.GizmoXYZ.Mod'; // ModSetting: assembly.namespace.class
 const patterns={GetSettingsLocaleID:()=>`Options.SECTION[${prefix}]`,GetBindingMapLocaleID:()=>`Options.INPUT_MAP[${prefix}]`,GetOptionGroupLocaleID:n=>`Options.GROUP[${prefix}.${n}]`,GetOptionLabelLocaleID:n=>`Options.OPTION[${prefix}.Settings.${n}]`,GetOptionDescLocaleID:n=>`Options.OPTION_DESCRIPTION[${prefix}.Settings.${n}]`,GetBindingKeyLocaleID:n=>`Options.OPTION[${prefix}/${n}/binding]`};
 for(const line of text.split(/\r?\n/).filter(l=>l.includes('[s.Get'))){
  const m=line.match(/\[s\.(\w+)\((.*?)\)\]\s*=\s*(.*?)[,]?\s*$/);
  if(!m||!patterns[m[1]])throw Error('Unsupported locale expression: '+line);
  const name=m[2].startsWith('nameof(')?m[2].match(/Settings\.(\w+)/)[1]:m[2]?JSON.parse(m[2]):'';
  const values=m[3].match(/^ko\s*\?\s*("(?:[^"\\]|\\.)*")\s*:\s*("(?:[^"\\]|\\.)*")$/);
  const key=patterns[m[1]](name);
  ko[key]=JSON.parse(values?values[1]:m[3]);en[key]=JSON.parse(values?values[2]:m[3]);
 }
 return {en,ko};
};
