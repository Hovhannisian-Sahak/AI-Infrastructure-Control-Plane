export type NodeMetric = {
    id: string;
    computeNodeId: string;
    cpuUsagePercent: number;
    gpuUsagePercent: number;
    gpuTemperatureCelsius: number;
    recordedAt: string;
};
