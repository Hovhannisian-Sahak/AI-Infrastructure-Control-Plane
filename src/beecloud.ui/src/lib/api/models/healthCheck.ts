export type HealthCheck = {
    id: string;
    computeNodeId: string;
    isHealthy: boolean;
    cpuUsagePercent: number | null;
    gpuUsagePercent: number | null;
    gpuTemperatureCelsius: number | null;
    checkedAt: string;
};