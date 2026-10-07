import {
  combineReducers,
  configureStore,
} from "@reduxjs/toolkit";
import {
  render,
} from "@testing-library/react";
import {
  Provider,
} from "react-redux";
import type {
  ReactElement,
  ReactNode,
} from "react";

import nodesReducer from "@/store/slices/nodesSlice";
import networksReducer from "@/store/slices/networksSlice";
import incidentsReducer from "@/store/slices/incidentsSlice";
import healthReducer from "@/store/slices/healthSlice";
import metricsReducer from "@/store/slices/metricsSlice";

const rootReducer = combineReducers({
  nodes: nodesReducer,
  networks: networksReducer,
  incidents: incidentsReducer,
  health: healthReducer,
  metrics: metricsReducer,
});

export type RootState = ReturnType<typeof rootReducer>;

type TestPreloadedState = {
  [Key in keyof RootState]?: Partial<RootState[Key]>;
};

export const createTestStore = (
    preloadedState?: TestPreloadedState,
) =>
    configureStore({
      reducer: rootReducer,
      preloadedState: {
        nodes: {
          ...nodesReducer(undefined, { type: "test/initialize" }),
          ...preloadedState?.nodes,
        },
        networks: {
          ...networksReducer(undefined, { type: "test/initialize" }),
          ...preloadedState?.networks,
        },
        incidents: {
          ...incidentsReducer(undefined, { type: "test/initialize" }),
          ...preloadedState?.incidents,
        },
        health: {
          ...healthReducer(undefined, { type: "test/initialize" }),
          ...preloadedState?.health,
        },
        metrics: {
          ...metricsReducer(undefined, { type: "test/initialize" }),
          ...preloadedState?.metrics,
        },
      },
    });

export type TestStore = ReturnType<
    typeof createTestStore
>;

export type AppDispatch =
    TestStore["dispatch"];

type RenderWithProvidersOptions = {
  preloadedState?: TestPreloadedState;
  store?: TestStore;
};

export function renderWithProviders(
    ui: ReactElement,
    {
      preloadedState,
      store = createTestStore(preloadedState),
    }: RenderWithProvidersOptions = {},
) {
  const Wrapper = ({
                     children,
                   }: {
    children: ReactNode;
  }) => (
      <Provider store={store}>
        {children}
      </Provider>
  );

  return {
    store,
    ...render(ui, {
      wrapper: Wrapper,
    }),
  };
}