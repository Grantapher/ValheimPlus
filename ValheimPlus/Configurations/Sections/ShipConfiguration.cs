using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ValheimPlus.Configurations.Sections
{
    // VikingShip is the prefab, behaviour is Ship
    public class ShipConfiguration : ServerSyncConfig<ShipConfiguration>
    {
        // m_force
        public float force { get; internal set; } = 0f;

        // m_stearForce
        public float steerForce { get; internal set; } = 0f;

        // m_waterImpactDamage, m_minWaterImpactInterval, m_minWaterImpactForce
        public float waterImpactDamage { get; internal set; } = 0f;

        // m_force, m_forceDistance, m_damping, m_dampingSideway, m_dampingForward, m_angularDamping

        // m_backwardForce
        public float backwardForce { get; internal set; } = 0f;

        // m_rudderSpeed
        public float rudderSpeed { get; internal set; } = 0f;
    }
}
