import assert from "assert/strict";

export default function smoothPeak(inArr, numParts) {

	const partialPeaks = partitionArray(inArr, numParts).map(
		arr => arr.reduce((a, b) => Math.max(a, b))
	);
	
	// return mean peak
	return partialPeaks.reduce((a, b) => a + b) / partialPeaks.length;
}

export function partitionArray(inArr, numParts){
	const parts = [];

	const arr = inArr.slice();
	const partSize = Math.round(arr.length / numParts);
	if(partSize < 100) {
		console.log(`warning: partSize=${partSize}`);
	}
	while (1) {
		const nextPart = arr.splice(0, partSize);
		if (nextPart.length == 0) {
			break;
		}
		parts.push(nextPart);
	}

	return parts;
}