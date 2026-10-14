using RushArena.Core;
using UnityEngine;

namespace RushArena.Multiplayer
{
    // Generates procedural sci-fi weapons, stylized combatant models, and floating 3D weapon drops.
    public static class ProceduralVisuals
    {
        public static GameObject BuildFirstPersonWeapon(Transform parent, WeaponType type, Material weaponMat, Material glowMat, out Transform muzzlePoint, out Light muzzleLight)
        {
            string weaponName = type.ToString();
            var root = new GameObject("FP_" + weaponName);
            root.transform.SetParent(parent, false);

            if (type == WeaponType.Pistol)
            {
                root.transform.localPosition = new Vector3(0.20f, -0.20f, 0.40f);
                root.transform.localRotation = Quaternion.identity;

                // Frame and Grip
                var grip = CreatePart(root.transform, "Grip", new Vector3(0f, -0.06f, -0.03f), new Vector3(0.034f, 0.09f, 0.045f), weaponMat);
                grip.transform.localRotation = Quaternion.Euler(-18f, 0f, 0f);
                CreatePart(root.transform, "TriggerGuard", new Vector3(0f, -0.035f, 0.015f), new Vector3(0.02f, 0.035f, 0.04f), weaponMat);

                // Slide & Barrel
                CreatePart(root.transform, "Slide", new Vector3(0f, 0.02f, 0.02f), new Vector3(0.042f, 0.05f, 0.19f), weaponMat);
                CreatePart(root.transform, "Barrel", new Vector3(0f, 0.02f, 0.12f), new Vector3(0.024f, 0.024f, 0.06f), weaponMat);

                // Glowing Tritium Night Sights
                CreatePart(root.transform, "FrontSight", new Vector3(0f, 0.05f, 0.10f), new Vector3(0.008f, 0.015f, 0.012f), glowMat);
                CreatePart(root.transform, "RearSightLeft", new Vector3(-0.014f, 0.05f, -0.065f), new Vector3(0.007f, 0.015f, 0.01f), glowMat);
                CreatePart(root.transform, "RearSightRight", new Vector3(0.014f, 0.05f, -0.065f), new Vector3(0.007f, 0.015f, 0.01f), glowMat);

                var muzzle = new GameObject("MuzzlePoint");
                muzzle.transform.SetParent(root.transform, false);
                muzzle.transform.localPosition = new Vector3(0f, 0.02f, 0.15f);
                muzzlePoint = muzzle.transform;
            }
            else if (type == WeaponType.AssaultRifle)
            {
                root.transform.localPosition = new Vector3(0.24f, -0.22f, 0.48f);
                root.transform.localRotation = Quaternion.identity;

                // Receiver body
                CreatePart(root.transform, "Receiver", new Vector3(0f, 0f, 0f), new Vector3(0.055f, 0.08f, 0.28f), weaponMat);
                CreatePart(root.transform, "Barrel", new Vector3(0f, 0.015f, 0.23f), new Vector3(0.022f, 0.022f, 0.22f), weaponMat);
                CreatePart(root.transform, "MuzzleBrake", new Vector3(0f, 0.015f, 0.35f), new Vector3(0.032f, 0.032f, 0.04f), weaponMat);
                CreatePart(root.transform, "TopRail", new Vector3(0f, 0.052f, 0.02f), new Vector3(0.03f, 0.02f, 0.24f), weaponMat);
                // Holographic Sight
                CreatePart(root.transform, "SightHousing", new Vector3(0f, 0.08f, 0.02f), new Vector3(0.038f, 0.045f, 0.06f), weaponMat);
                CreatePart(root.transform, "SightReticle", new Vector3(0f, 0.08f, 0.04f), new Vector3(0.015f, 0.015f, 0.005f), glowMat);
                // Magazine & Grip
                var mag = CreatePart(root.transform, "Magazine", new Vector3(0f, -0.075f, 0.06f), new Vector3(0.035f, 0.12f, 0.06f), weaponMat);
                mag.transform.localRotation = Quaternion.Euler(14f, 0f, 0f);
                var grip = CreatePart(root.transform, "Grip", new Vector3(0f, -0.07f, -0.08f), new Vector3(0.035f, 0.10f, 0.05f), weaponMat);
                grip.transform.localRotation = Quaternion.Euler(-18f, 0f, 0f);
                CreatePart(root.transform, "Stock", new Vector3(0f, -0.01f, -0.20f), new Vector3(0.04f, 0.09f, 0.16f), weaponMat);

                var muzzle = new GameObject("MuzzlePoint");
                muzzle.transform.SetParent(root.transform, false);
                muzzle.transform.localPosition = new Vector3(0f, 0.015f, 0.38f);
                muzzlePoint = muzzle.transform;
            }
            else if (type == WeaponType.PlasmaCannon)
            {
                root.transform.localPosition = new Vector3(0.24f, -0.22f, 0.48f);
                root.transform.localRotation = Quaternion.identity;

                // Heavy chassis
                CreatePart(root.transform, "Chassis", new Vector3(0f, 0f, 0f), new Vector3(0.08f, 0.10f, 0.32f), weaponMat);
                CreatePart(root.transform, "UpperRail", new Vector3(0f, 0.06f, 0.18f), new Vector3(0.06f, 0.025f, 0.28f), weaponMat);
                CreatePart(root.transform, "LowerRail", new Vector3(0f, -0.06f, 0.18f), new Vector3(0.06f, 0.025f, 0.28f), weaponMat);
                // Glowing plasma core
                CreatePart(root.transform, "PlasmaCore", new Vector3(0f, 0f, 0.05f), new Vector3(0.045f, 0.045f, 0.18f), glowMat);
                CreatePart(root.transform, "EmitterRing", new Vector3(0f, 0f, 0.33f), new Vector3(0.09f, 0.09f, 0.035f), glowMat);
                var grip = CreatePart(root.transform, "Grip", new Vector3(0f, -0.09f, -0.06f), new Vector3(0.04f, 0.11f, 0.05f), weaponMat);
                grip.transform.localRotation = Quaternion.Euler(-15f, 0f, 0f);

                var muzzle = new GameObject("MuzzlePoint");
                muzzle.transform.SetParent(root.transform, false);
                muzzle.transform.localPosition = new Vector3(0f, 0f, 0.36f);
                muzzlePoint = muzzle.transform;
            }
            else // Scattergun (Shotgun)
            {
                root.transform.localPosition = new Vector3(0.22f, -0.20f, 0.44f);
                root.transform.localRotation = Quaternion.identity;

                // Heavy Shotgun Receiver & Twin Barrel
                CreatePart(root.transform, "Receiver", new Vector3(0f, 0f, -0.04f), new Vector3(0.065f, 0.085f, 0.26f), weaponMat);
                CreatePart(root.transform, "TwinBarrelLeft", new Vector3(-0.018f, 0.015f, 0.20f), new Vector3(0.028f, 0.028f, 0.28f), weaponMat);
                CreatePart(root.transform, "TwinBarrelRight", new Vector3(0.018f, 0.015f, 0.20f), new Vector3(0.028f, 0.028f, 0.28f), weaponMat);
                // Vented Heat Shield
                CreatePart(root.transform, "HeatShield", new Vector3(0f, 0.038f, 0.16f), new Vector3(0.075f, 0.025f, 0.22f), glowMat);
                // Pump grip
                CreatePart(root.transform, "PumpGrip", new Vector3(0f, -0.035f, 0.14f), new Vector3(0.055f, 0.045f, 0.12f), weaponMat);
                // Pistol Grip
                var grip = CreatePart(root.transform, "Grip", new Vector3(0f, -0.08f, -0.10f), new Vector3(0.038f, 0.10f, 0.055f), weaponMat);
                grip.transform.localRotation = Quaternion.Euler(-22f, 0f, 0f);

                var muzzle = new GameObject("MuzzlePoint");
                muzzle.transform.SetParent(root.transform, false);
                muzzle.transform.localPosition = new Vector3(0f, 0.015f, 0.35f);
                muzzlePoint = muzzle.transform;
            }

            var lightObj = new GameObject("MuzzleFlashLight");
            lightObj.transform.SetParent(muzzlePoint, false);
            muzzleLight = lightObj.AddComponent<Light>();
            muzzleLight.type = LightType.Point;
            muzzleLight.range = 4.0f;
            muzzleLight.intensity = 2.8f;
            muzzleLight.color = type == WeaponType.PlasmaCannon ? new Color(0.2f, 0.85f, 1f) : new Color(1f, 0.85f, 0.3f);
            muzzleLight.enabled = false;

            return root;
        }

        // Creates a rotating 3D weapon pickup model for arena pedestals
        public static GameObject BuildWeaponPickupModel(Transform parent, WeaponType type, Material weaponMat, Material glowMat)
        {
            var root = new GameObject("PickupModel_" + type);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = new Vector3(0f, 0.35f, 0f);
            root.transform.localScale = Vector3.one * 1.35f;

            if (type == WeaponType.AssaultRifle)
            {
                CreatePart(root.transform, "Body", Vector3.zero, new Vector3(0.06f, 0.09f, 0.38f), weaponMat);
                CreatePart(root.transform, "Barrel", new Vector3(0f, 0.02f, 0.28f), new Vector3(0.025f, 0.025f, 0.22f), weaponMat);
                CreatePart(root.transform, "Mag", new Vector3(0f, -0.09f, 0.05f), new Vector3(0.04f, 0.13f, 0.06f), glowMat);
                CreatePart(root.transform, "Sight", new Vector3(0f, 0.08f, 0.02f), new Vector3(0.04f, 0.04f, 0.07f), glowMat);
            }
            else if (type == WeaponType.PlasmaCannon)
            {
                CreatePart(root.transform, "Body", Vector3.zero, new Vector3(0.10f, 0.12f, 0.40f), weaponMat);
                CreatePart(root.transform, "RailU", new Vector3(0f, 0.07f, 0.18f), new Vector3(0.07f, 0.03f, 0.28f), weaponMat);
                CreatePart(root.transform, "RailD", new Vector3(0f, -0.07f, 0.18f), new Vector3(0.07f, 0.03f, 0.28f), weaponMat);
                CreatePart(root.transform, "Core", new Vector3(0f, 0f, 0.05f), new Vector3(0.06f, 0.06f, 0.20f), glowMat);
                CreatePart(root.transform, "Ring", new Vector3(0f, 0f, 0.32f), new Vector3(0.11f, 0.11f, 0.04f), glowMat);
            }
            else if (type == WeaponType.Scattergun)
            {
                CreatePart(root.transform, "Body", Vector3.zero, new Vector3(0.08f, 0.10f, 0.32f), weaponMat);
                CreatePart(root.transform, "Barrels", new Vector3(0f, 0.02f, 0.22f), new Vector3(0.06f, 0.03f, 0.26f), weaponMat);
                CreatePart(root.transform, "Shield", new Vector3(0f, 0.05f, 0.15f), new Vector3(0.09f, 0.03f, 0.20f), glowMat);
                CreatePart(root.transform, "Pump", new Vector3(0f, -0.04f, 0.12f), new Vector3(0.06f, 0.05f, 0.12f), weaponMat);
            }

            return root;
        }

        // Builds an athletic, stylized 3D sci-fi combatant gladiator
        public static GameObject BuildThirdPersonCharacter(Transform root, Material armorMat, Material visorMat, Material weaponMat,
            out Transform upperBody, out Transform leftLeg, out Transform rightLeg, out Transform tpWeapon)
        {
            var hips = new GameObject("Avatar_Hips");
            hips.transform.SetParent(root, false);
            hips.transform.localPosition = new Vector3(0f, 0.88f, 0f);

            // Tactical Utility Belt & Hip Guards
            CreatePart(hips.transform, "Belt", new Vector3(0f, 0.02f, 0f), new Vector3(0.38f, 0.08f, 0.24f), armorMat);
            CreatePart(hips.transform, "BeltBuckle", new Vector3(0f, 0.02f, 0.13f), new Vector3(0.09f, 0.06f, 0.02f), visorMat);
            CreatePart(hips.transform, "PouchLeft", new Vector3(-0.21f, 0f, 0.02f), new Vector3(0.06f, 0.08f, 0.10f), armorMat);
            CreatePart(hips.transform, "PouchRight", new Vector3(0.21f, 0f, 0.02f), new Vector3(0.06f, 0.08f, 0.10f), armorMat);

            // Upper body pivot (rotates with look pitch)
            var torsoPivot = new GameObject("Avatar_UpperBody");
            torsoPivot.transform.SetParent(hips.transform, false);
            torsoPivot.transform.localPosition = Vector3.zero;
            upperBody = torsoPivot.transform;

            // Segmented Torso Armor (Athletic Sci-Fi Profile)
            CreatePart(upperBody, "AbPlate", new Vector3(0f, 0.14f, 0f), new Vector3(0.36f, 0.18f, 0.22f), armorMat);
            CreatePart(upperBody, "ChestPlate", new Vector3(0f, 0.32f, 0.02f), new Vector3(0.46f, 0.28f, 0.26f), armorMat);
            // Glowing Arc Reactor Core
            CreatePart(upperBody, "CoreReactor", new Vector3(0f, 0.34f, 0.16f), new Vector3(0.13f, 0.13f, 0.02f), visorMat);

            // Angular Jetpack Thrusters with Neon Nozzles
            var pack = CreatePart(upperBody, "JetpackBody", new Vector3(0f, 0.30f, -0.16f), new Vector3(0.32f, 0.30f, 0.08f), armorMat);
            CreatePart(pack.transform, "ThrusterL", new Vector3(-0.11f, 0f, -0.05f), new Vector3(0.08f, 0.26f, 0.08f), armorMat);
            CreatePart(pack.transform, "ThrusterR", new Vector3(0.11f, 0f, -0.05f), new Vector3(0.08f, 0.26f, 0.08f), armorMat);
            CreatePart(pack.transform, "NozzleL", new Vector3(-0.11f, -0.14f, -0.05f), new Vector3(0.065f, 0.03f, 0.065f), visorMat);
            CreatePart(pack.transform, "NozzleR", new Vector3(0.11f, -0.14f, -0.05f), new Vector3(0.065f, 0.03f, 0.065f), visorMat);

            // Cyberpunk Spartan-style Helmet with Faceted Glowing Visor
            var head = CreatePart(upperBody, "HelmetBase", new Vector3(0f, 0.62f, 0f), new Vector3(0.24f, 0.26f, 0.24f), armorMat);
            CreatePart(head.transform, "CheekL", new Vector3(-0.11f, -0.04f, 0.06f), new Vector3(0.04f, 0.12f, 0.08f), armorMat);
            CreatePart(head.transform, "CheekR", new Vector3(0.11f, -0.04f, 0.06f), new Vector3(0.04f, 0.12f, 0.08f), armorMat);
            CreatePart(head.transform, "ForeheadCrest", new Vector3(0f, 0.11f, 0.04f), new Vector3(0.12f, 0.04f, 0.18f), armorMat);
            CreatePart(head.transform, "VisorGlow", new Vector3(0f, 0.02f, 0.125f), new Vector3(0.20f, 0.06f, 0.02f), visorMat);

            // Armored Pauldrons (Shoulders) & Gauntlets
            CreatePart(upperBody, "Pauldrons_R", new Vector3(0.30f, 0.40f, 0.02f), new Vector3(0.14f, 0.12f, 0.16f), armorMat);
            CreatePart(upperBody, "Arm_R", new Vector3(0.26f, 0.28f, 0.20f), new Vector3(0.09f, 0.09f, 0.26f), armorMat);
            CreatePart(upperBody, "Gauntlet_R", new Vector3(0.26f, 0.28f, 0.28f), new Vector3(0.10f, 0.10f, 0.12f), visorMat);

            CreatePart(upperBody, "Pauldrons_L", new Vector3(-0.30f, 0.40f, 0.02f), new Vector3(0.14f, 0.12f, 0.16f), armorMat);
            CreatePart(upperBody, "Arm_L", new Vector3(-0.18f, 0.26f, 0.26f), new Vector3(0.09f, 0.09f, 0.28f), armorMat);

            // Third-person weapon
            var weaponHolder = new GameObject("TP_WeaponHolder");
            weaponHolder.transform.SetParent(upperBody, false);
            weaponHolder.transform.localPosition = new Vector3(0.16f, 0.24f, 0.42f);
            CreatePart(weaponHolder.transform, "GunChassis", Vector3.zero, new Vector3(0.06f, 0.09f, 0.44f), weaponMat);
            CreatePart(weaponHolder.transform, "GunBarrel", new Vector3(0f, 0.02f, 0.28f), new Vector3(0.03f, 0.03f, 0.24f), weaponMat);
            CreatePart(weaponHolder.transform, "GunGlow", new Vector3(0f, 0.04f, 0.05f), new Vector3(0.03f, 0.02f, 0.16f), visorMat);
            tpWeapon = weaponHolder.transform;

            // Articulated Armored Legs
            var leftLegObj = new GameObject("Leg_L");
            leftLegObj.transform.SetParent(hips.transform, false);
            leftLegObj.transform.localPosition = new Vector3(-0.13f, -0.06f, 0f);
            CreatePart(leftLegObj.transform, "Thigh_L", new Vector3(0f, -0.20f, 0f), new Vector3(0.13f, 0.36f, 0.15f), armorMat);
            CreatePart(leftLegObj.transform, "Knee_L", new Vector3(0f, -0.38f, 0.07f), new Vector3(0.11f, 0.08f, 0.04f), visorMat);
            CreatePart(leftLegObj.transform, "Shin_L", new Vector3(0f, -0.56f, 0.02f), new Vector3(0.12f, 0.36f, 0.14f), armorMat);
            CreatePart(leftLegObj.transform, "Boot_L", new Vector3(0f, -0.78f, 0.08f), new Vector3(0.13f, 0.10f, 0.24f), armorMat);
            leftLeg = leftLegObj.transform;

            var rightLegObj = new GameObject("Leg_R");
            rightLegObj.transform.SetParent(hips.transform, false);
            rightLegObj.transform.localPosition = new Vector3(0.13f, -0.06f, 0f);
            CreatePart(rightLegObj.transform, "Thigh_R", new Vector3(0f, -0.20f, 0f), new Vector3(0.13f, 0.36f, 0.15f), armorMat);
            CreatePart(rightLegObj.transform, "Knee_R", new Vector3(0f, -0.38f, 0.07f), new Vector3(0.11f, 0.08f, 0.04f), visorMat);
            CreatePart(rightLegObj.transform, "Shin_R", new Vector3(0f, -0.56f, 0.02f), new Vector3(0.12f, 0.36f, 0.14f), armorMat);
            CreatePart(rightLegObj.transform, "Boot_R", new Vector3(0f, -0.78f, 0.08f), new Vector3(0.13f, 0.10f, 0.24f), armorMat);
            rightLeg = rightLegObj.transform;

            return hips;
        }

        private static GameObject CreatePart(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat)
        {
            var part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = pos;
            part.transform.localScale = scale;
            Object.Destroy(part.GetComponent<Collider>());
            if (mat != null) part.GetComponent<Renderer>().sharedMaterial = mat;
            return part;
        }
    }
}
