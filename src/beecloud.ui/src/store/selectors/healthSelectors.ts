import type { RootState } from "@/store/store";
import type { HealthCheck } from "@/lib/api/models/healthCheck";

const EMPTY_HEALTH_HISTORY: HealthCheck[] = [];

export const selectHealthHistoryByNodeId = (
    state: RootState,
    nodeId: string,
): HealthCheck[] =>
    state.health.historyByNodeId[nodeId] ??
    EMPTY_HEALTH_HISTORY;

export const selectLatestHealthByNodeId = (
    state: RootState,
    nodeId: string,
): HealthCheck | null =>
    state.health.latestByNodeId[nodeId] ?? null;

export const selectHealthLoadingByNodeId = (
    state: RootState,
    nodeId: string,
): boolean =>
    state.health.loadingByNodeId[nodeId] ?? false;

export const selectHealthErrorByNodeId = (
    state: RootState,
    nodeId: string,
): string | null =>
    state.health.errorByNodeId[nodeId] ?? null;

export const selectHealthyHealthChecks = (
    history: HealthCheck[],
): HealthCheck[] =>
    history.filter(item => item.isHealthy);

export const selectUnhealthyHealthChecks = (
    history: HealthCheck[],
): HealthCheck[] =>
    history.filter(item => !item.isHealthy);

export const selectCpuHistory = (
    history: HealthCheck[],
): Array<number | null> =>
    history.map(item => item.cpuUsagePercent);

export const selectGpuHistory = (
    history: HealthCheck[],
): Array<number | null> =>
    history.map(item => item.gpuUsagePercent);

export const selectTemperatureHistory = (
    history: HealthCheck[],
): Array<number | null> =>
    history.map(item => item.gpuTemperatureCelsius);

export const selectHealthHistorySince = (
    history: HealthCheck[],
    start: Date,
): HealthCheck[] =>
    history.filter(
        item =>
            new Date(item.checkedAt).getTime() >=
            start.getTime(),
    );