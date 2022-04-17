export default (obj) => {
    const refs = Object.fromEntries(
        obj.references.RefIds.map((reference) => [
            reference.rid,
            reference.data,
        ])
    );

    console.dir(refs);

    return obj;
};
