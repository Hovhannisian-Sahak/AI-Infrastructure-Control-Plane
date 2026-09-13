using BeeCloud.Domain.Enums;

namespace BeeCloud.Application.DTOs.Simulation;

public class SimulateFaultRequest
{
    public NodeFault Fault { get; set; }
}