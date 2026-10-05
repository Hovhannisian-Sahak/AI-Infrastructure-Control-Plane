import { configureStore } from "@reduxjs/toolkit";
import nodesReducer from "./slices/nodesSlice";
import networksReducer from "./slices/networksSlice";
import incidentsReducer from "./slices/incidentsSlice";
export const store = configureStore({
    reducer: {
        nodes: nodesReducer,
        networks: networksReducer,
        incidents: incidentsReducer,
    },
});

export type RootState = ReturnType<
    typeof store.getState
>;

export type AppDispatch = typeof store.dispatch;