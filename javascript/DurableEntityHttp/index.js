﻿const df = require("durable-functions");

module.exports = async function (context, req) {
    const client = df.getClient(context);
    const id = context.bindingData.id;
    const entityId = new df.EntityId("Counter", id);
    const operation = req.query.operation || "";

    if (operation === "") {
        // retrieve current value
        const stateResponse = await client.readEntityState(entityId);
        if (stateResponse.entityExists) {
            return { body: stateResponse.entityState };
        } else {
            return { status: 404 };
        }
    } else if (operation === "increment") {
        // increment value
        await client.signalEntity(entityId, "add", 1);
        return { body: "OK" };
    } else if (operation === "reset") {
        // reset value
        await client.signalEntity(entityId, "reset");
        return { body: "OK" };
    }

    return { status: 400 };
};