import { configureStore } from "@reduxjs/toolkit";
import nodesReducer from "./slices/nodesSlice";
import networksReducer from "./slices/networksSlice";

export const store = configureStore({
    reducer: {
        nodes: nodesReducer,
        networks: networksReducer,
    },
});

export type RootState = ReturnType<
    typeof store.getState
>;

export type AppDispatch = typeof store.dispatch;