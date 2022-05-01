import fs from 'fs';

function zip(...rows) {
    if (rows.length == 0) {
        return [];
    }
    return [...rows[0].keys()].map((columnIndex) =>
        rows.map((row) => row[columnIndex])
    );
}

function transpose(arr) {
    return [...zip(...arr)];
}

export default function loadDataset({
    path,
    maxSamples,
}: {
    path: string;
    maxSamples: number;
}) {
    let samples = fs
        .readFileSync(path, 'utf-8')
        // get rows
        .split('\n')
        .map(
            // split columns by space
            (line) =>
                line
                    .split(' ')
                    // drop empty columns
                    .filter((linePart) => linePart != '')
                    .map(
                        // convert strings to numbers
                        (s) => Number.parseFloat(s)
                    )
        )
        .filter(
            // drop rows with invalid number of columns (e.g. empty)
            (row) => row.length == 2
        );

    // normalize
    {
        samples = transpose(
            transpose(samples).map((column) => {
                const min = column.reduce((a, b) => Math.min(a, b));
                const max = column.reduce((a, b) => Math.max(a, b));
                const range = max - min;
                return column.map((x) => (x - min) / range);
            })
        );
    }

    // select random subset, if the number of samples exceeds maxSamples
    {
        if (maxSamples == undefined || samples.length <= maxSamples) {
            return samples;
        }

        return samples.filter(
            () => Math.random() < maxSamples / samples.length
        );
    }
}
