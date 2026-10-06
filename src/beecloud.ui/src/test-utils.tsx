import { render } from "@testing-library/react";
import { Provider } from "react-redux";
import { configureStore } from "@reduxjs/toolkit";
import type { ReactElement, ReactNode } from "react";

import nodesReducer from "@/store/slices/nodesSlice";
import networksReducer from "@/store/slices/networksSlice";
import indicentReducer from "@/store/slices/incidentsSlice";

type PreloadedState = {
  nodes?: {
    nodes?: ReturnType<typeof nodesReducer>["nodes"];
    loading?: boolean;
    creating?: boolean;
    refreshing?: boolean;
    createSuccess?: string | null;
    actionLoadingByNodeId?: Record<string, boolean>;
    error?: string | null;
  };
  networks?: Partial<ReturnType<typeof networksReducer>>;
  incidents?: Partial<ReturnType<typeof indicentReducer>>;
};

export function createTestStore(preloadedState?: PreloadedState) {
  const defaultNodesState = nodesReducer(undefined, {
    type: "@@INIT",
  });

  const defaultNetworksState = networksReducer(undefined, {
    type: "@@INIT",
  });
  
  const defaultIncidentState = indicentReducer(undefined, {
    type: "@@INIT",
  });

  return configureStore({
    reducer: {
      nodes: nodesReducer,
      networks: networksReducer,
      incidents: indicentReducer,
    },
    preloadedState: {
      nodes: {
        ...defaultNodesState,
        ...preloadedState?.nodes,
      },
      networks: {
        ...defaultNetworksState,
        ...preloadedState?.networks,
      },
      incidents: {
        ...defaultIncidentState,
        ...preloadedState?.incidents,
      },
    },
  });
}

export function renderWithProviders(ui: ReactElement, preloadedState?: PreloadedState) {
  const store = createTestStore(preloadedState);

  function Wrapper({ children }: { children: ReactNode }) {
    return <Provider store={store}>{children}</Provider>;
  }

  return {
    ...render(ui, {
      wrapper: Wrapper,
    }),
    store,
  };
}
