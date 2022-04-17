export default (obj) => {
    const refs = Object.fromEntries(
        obj.references.RefIds.map((reference) => [
            reference.rid,
            reference.data,
        ])
    );

    return 1;

    return obj.references;
};
