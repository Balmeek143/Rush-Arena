using RushArena.Core;
using UnityEngine;

namespace RushArena.Multiplayer
{
    // Generates procedural low-poly sci-fi weapons and stylized combatant models.
    // Provides immediate visual polish, silhouettes, and aiming replication without external 3D model files.
    public static class ProceduralVisuals
    {
        public static GameObject BuildFirstPersonWeapon(Transform parent, WeaponType type, Material weaponMat, Material glowMat, out Transform muzzlePoint, out Light muzzleLight)
        {
            var root = new GameObject(type == WeaponType.AssaultRifle ? "FP_Rifle" : "FP_Plasma");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = new Vector3(0.24f, -0.22f, 0.48f);
            root.transform.localRotation = Quaternion.identity;

            if (type == WeaponType.AssaultRifle)
            {
                // Receiver body
                CreatePart(root.transform, "Receiver", new Vector3(0f, 0f, 0f), new Vector3(0.055f, 0.08f, 0.28f), weaponMat);
                // Barrel
                CreatePart(root.transform, "Barrel", new Vector3(0f, 0.015f, 0.23f), new Vector3(0.022f, 0.022f, 0.22f), weaponMat);
                // Muzzle brake
                CreatePart(root.transform, "MuzzleBrake", new Vector3(0f, 0.015f, 0.35f), new Vector3(0.032f, 0.032f, 0.04f), weaponMat);
                // Top rail
                CreatePart(root.transform, "TopRail", new Vector3(0f, 0.052f, 0.02f), new Vector3(0.03f, 0.02f, 0.24f), weaponMat);
                // Holographic Sight
                CreatePart(root.transform, "SightHousing", new Vector3(0f, 0.08f, 0.02f), new Vector3(0.038f, 0.045f, 0.06f), weaponMat);
                var reticle = CreatePart(root.transform, "SightReticle", new Vector3(0f, 0.08f, 0.04f), new Vector3(0.015f, 0.015f, 0.005f), glowMat);
                // Curved magazine
                var mag = CreatePart(root.transform, "Magazine", new Vector3(0f, -0.075f, 0.06f), new Vector3(0.035f, 0.12f, 0.06f), weaponMat);
                mag.transform.localRotation = Quaternion.Euler(14f, 0f, 0f);
                // Grip
                var grip = CreatePart(root.transform, "Grip", new Vector3(0f, -0.07f, -0.08f), new Vector3(0.035f, 0.10f, 0.05f), weaponMat);
                grip.transform.localRotation = Quaternion.Euler(-18f, 0f, 0f);
                // Stock
                CreatePart(root.transform, "Stock", new Vector3(0f, -0.01f, -0.20f), new Vector3(0.04f, 0.09f, 0.16f), weaponMat);

                // Muzzle position
                var muzzle = new GameObject("MuzzlePoint");
                muzzle.transform.SetParent(root.transform, false);
                muzzle.transform.localPosition = new Vector3(0f, 0.015f, 0.38f);
                muzzlePoint = muzzle.transform;
            }
            else // Plasma Cannon
            {
                // Heavy chassis
                CreatePart(root.transform, "Chassis", new Vector3(0f, 0f, 0f), new Vector3(0.08f, 0.10f, 0.32f), weaponMat);
                // Twin rails
                CreatePart(root.transform, "UpperRail", new Vector3(0f, 0.06f, 0.18f), new Vector3(0.06f, 0.025f, 0.28f), weaponMat);
                CreatePart(root.transform, "LowerRail", new Vector3(0f, -0.06f, 0.18f), new Vector3(0.06f, 0.025f, 0.28f), weaponMat);
                // Glowing plasma core
                CreatePart(root.transform, "PlasmaCore", new Vector3(0f, 0f, 0.05f), new Vector3(0.045f, 0.045f, 0.18f), glowMat);
                // Emitter ring
                CreatePart(root.transform, "EmitterRing", new Vector3(0f, 0f, 0.33f), new Vector3(0.09f, 0.09f, 0.035f), glowMat);
                // Grip
                var grip = CreatePart(root.transform, "Grip", new Vector3(0f, -0.09f, -0.06f), new Vector3(0.04f, 0.11f, 0.05f), weaponMat);
                grip.transform.localRotation = Quaternion.Euler(-15f, 0f, 0f);

                var muzzle = new GameObject("MuzzlePoint");
                muzzle.transform.SetParent(root.transform, false);
                muzzle.transform.localPosition = new Vector3(0f, 0f, 0.36f);
                muzzlePoint = muzzle.transform;
            }

            var lightObj = new GameObject("MuzzleFlashLight");
            lightObj.transform.SetParent(muzzlePoint, false);
            muzzleLight = lightObj.AddComponent<Light>();
            muzzleLight.type = LightType.Point;
            muzzleLight.range = 3.5f;
            muzzleLight.intensity = 2.5f;
            muzzleLight.color = type == WeaponType.AssaultRifle ? new Color(1f, 0.8f, 0.3f) : new Color(0.2f, 0.8f, 1f);
            muzzleLight.enabled = false;

            return root;
        }

        public static GameObject BuildThirdPersonCharacter(Transform root, Material armorMat, Material visorMat, Material weaponMat, out Transform upperBody, out Transform leftLeg, out Transform rightLeg, out Transform tpWeapon)
        {
            var hips = new GameObject("Avatar_Hips");
            hips.transform.SetParent(root, false);
            hips.transform.localPosition = new Vector3(0f, 0.9f, 0f);

            // Upper body pivot (rotates with look pitch)
            var torsoPivot = new GameObject("Avatar_UpperBody");
            torsoPivot.transform.SetParent(hips.transform, false);
            torsoPivot.transform.localPosition = Vector3.zero;
            upperBody = torsoPivot.transform;

            // Torso chest armor
            CreatePart(upperBody, "ChestArmor", new Vector3(0f, 0.28f, 0f), new Vector3(0.48f, 0.52f, 0.28f), armorMat);
            // Chest core emblem
            CreatePart(upperBody, "CoreEmblem", new Vector3(0f, 0.32f, 0.15f), new Vector3(0.14f, 0.14f, 0.02f), visorMat);
            // Jetpack thrusters on back
            CreatePart(upperBody, "ThrusterLeft", new Vector3(-0.14f, 0.30f, -0.18f), new Vector3(0.09f, 0.32f, 0.09f), armorMat);
            CreatePart(upperBody, "ThrusterRight", new Vector3(0.14f, 0.30f, -0.18f), new Vector3(0.09f, 0.32f, 0.09f), armorMat);

            // Helmet & Glowing Visor
            var head = CreatePart(upperBody, "Helmet", new Vector3(0f, 0.68f, 0f), new Vector3(0.26f, 0.28f, 0.26f), armorMat);
            CreatePart(head.transform, "Visor", new Vector3(0f, 0.02f, 0.135f), new Vector3(0.22f, 0.07f, 0.02f), visorMat);

            // Arms holding weapon forward
            // Right arm (trigger arm)
            CreatePart(upperBody, "RightShoulder", new Vector3(0.30f, 0.42f, 0.05f), new Vector3(0.12f, 0.14f, 0.14f), armorMat);
            CreatePart(upperBody, "RightForearm", new Vector3(0.28f, 0.30f, 0.24f), new Vector3(0.10f, 0.10f, 0.30f), armorMat);
            // Left arm (supporting handguard)
            CreatePart(upperBody, "LeftShoulder", new Vector3(-0.30f, 0.42f, 0.05f), new Vector3(0.12f, 0.14f, 0.14f), armorMat);
            CreatePart(upperBody, "LeftForearm", new Vector3(-0.16f, 0.28f, 0.32f), new Vector3(0.10f, 0.10f, 0.32f), armorMat);

            // Third-person weapon in hands
            var weaponHolder = new GameObject("TP_WeaponHolder");
            weaponHolder.transform.SetParent(upperBody, false);
            weaponHolder.transform.localPosition = new Vector3(0.18f, 0.26f, 0.45f);
            CreatePart(weaponHolder.transform, "TP_RifleBody", Vector3.zero, new Vector3(0.07f, 0.10f, 0.46f), weaponMat);
            CreatePart(weaponHolder.transform, "TP_RifleBarrel", new Vector3(0f, 0.02f, 0.30f), new Vector3(0.03f, 0.03f, 0.24f), weaponMat);
            CreatePart(weaponHolder.transform, "TP_Muzzle", new Vector3(0f, 0.02f, 0.42f), new Vector3(0.045f, 0.045f, 0.04f), weaponMat);
            tpWeapon = weaponHolder.transform;

            // Legs
            var leftLegObj = new GameObject("Leg_L");
            leftLegObj.transform.SetParent(hips.transform, false);
            leftLegObj.transform.localPosition = new Vector3(-0.14f, -0.05f, 0f);
            CreatePart(leftLegObj.transform, "Thigh_L", new Vector3(0f, -0.22f, 0f), new Vector3(0.14f, 0.38f, 0.16f), armorMat);
            CreatePart(leftLegObj.transform, "Shin_L", new Vector3(0f, -0.58f, 0.02f), new Vector3(0.13f, 0.38f, 0.15f), armorMat);
            CreatePart(leftLegObj.transform, "Boot_L", new Vector3(0f, -0.80f, 0.08f), new Vector3(0.14f, 0.10f, 0.24f), armorMat);
            leftLeg = leftLegObj.transform;

            var rightLegObj = new GameObject("Leg_R");
            rightLegObj.transform.SetParent(hips.transform, false);
            rightLegObj.transform.localPosition = new Vector3(0.14f, -0.05f, 0f);
            CreatePart(rightLegObj.transform, "Thigh_R", new Vector3(0f, -0.22f, 0f), new Vector3(0.14f, 0.38f, 0.16f), armorMat);
            CreatePart(rightLegObj.transform, "Shin_R", new Vector3(0f, -0.58f, 0.02f), new Vector3(0.13f, 0.38f, 0.15f), armorMat);
            CreatePart(rightLegObj.transform, "Boot_R", new Vector3(0f, -0.80f, 0.08f), new Vector3(0.14f, 0.10f, 0.24f), armorMat);
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
