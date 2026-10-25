# sucrase.js

Bundle de [Sucrase](https://github.com/alangpierce/sucrase) 3.35.1 (licence MIT), exécuté dans Jint par `SucraseTypeScriptTranspiler` pour retirer les types TypeScript.

Seule la fonction `transform` est exposée, sous `globalThis.sucrase`. Pour régénérer le bundle :

```bash
npm i sucrase@3.35.1 esbuild
echo 'import { transform } from "sucrase"; globalThis.sucrase = { transform };' > entry.js
npx esbuild entry.js --bundle --format=iife --minify --target=es2019 --platform=neutral --main-fields=main,module --legal-comments=none --outfile=sucrase.js
```
