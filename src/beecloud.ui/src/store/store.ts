import { configureStore } from "@reduxjs/toolkit";
import nodesReducer from "./slices/nodesSlice";

export const store = configureStore({
    reducer: {
        nodes: nodesReducer,
    },
});

export type RootState = ReturnType<typeof store.getState>;
export type AppDispatch = typeof store.dispatch;