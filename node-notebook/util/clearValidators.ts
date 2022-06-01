import fs from 'fs/promises';
import path from 'path';

const dirPath = './util/validators/generated';

(async () => {
    for (const file of await fs.readdir(dirPath)) {
        await fs.rm(path.join(dirPath, file));
    }
})();