import assert from "assert/strict";

export default function smoothPeak(inArr) {
	const arr = inArr.slice();
	const numParts = 100;
	const partSize = Math.round(arr.length / numParts);
	//assert.ok(partSize > 100);
	const partialPeaks = [];
	while (1) {
		const nextPart = arr.splice(0, partSize);
		if (nextPart.length == 0) {
			break;
		}
		partialPeaks.push(nextPart.reduce((a, b) => Math.max(a, b)));
	}
	return partialPeaks.reduce((a, b) => a + b) / partialPeaks.length;
}