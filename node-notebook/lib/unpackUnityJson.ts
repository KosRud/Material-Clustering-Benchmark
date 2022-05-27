import assert from 'assert/strict';

export default function unpackUnityJson(obj) {
    assert(obj.references);

    const refs = Object.fromEntries(
        obj.references.RefIds.map((reference) => [
            reference.rid,
            reference.data,
        ])
    );

    function fillRefs(obj) {
        let nothingChanged = true;

        do {
            nothingChanged = true;

            for (const attr in obj) {
                const val = obj[attr];
                if (typeof val == 'object') {
                    if (val.rid) {
                        obj[attr] = refs[val.rid];
                        nothingChanged = false;
                    } else {
                        fillRefs(val);
                    }
                }
            }
        } while (nothingChanged == false);

        return obj;
    }

    return fillRefs(obj);
}
