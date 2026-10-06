export type NodeStatus =
    | "Provisioning"
    | "Available"
    | "Running"
    | "Stopping"
    | "Stopped"
    | "Unhealthy"
    | "Quarantined"
    | "Remediating"
    | "Failed";

export type ActiveFault =
    | "None"
    | "GpuFailure"
    | "GpuOverheat"
    | "NetworkFailure"
    | "ServiceCrash";

export type HealthCheck = {
  isHealthy: boolean;
  cpuUsagePercent: number | null;
  gpuUsagePercent: number | null;
  gpuTemperatureCelsius: number | null;
  checkedAt: string;
};

export type ComputeNode = {
  id: string;
  name: string;
  gpuModel: string;
  gpuCount: number;
  status: NodeStatus;
  activeFault: ActiveFault;
  lastHealthCheck: string | null;
  healthCheck: HealthCheck | null;
};