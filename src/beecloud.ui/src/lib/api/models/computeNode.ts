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

export type ActiveFault = "None" | "GpuFailure" | "GpuOverheat" | "NetworkFailure" | "ServiceCrash";

export type ComputeNode = {
  id: string;
  name: string;
  gpuModel: string;
  gpuCount: number;
  status: NodeStatus;
  activeFault: ActiveFault;
};
