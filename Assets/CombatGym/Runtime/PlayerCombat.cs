using System;
using Jimmy.Movement;
using UnityEngine;
using UnityEngine.InputSystem;
namespace Jimmy.Combat
{
    [DefaultExecutionOrder(20)]
    [RequireComponent(typeof(RigidbodyMotor), typeof(FpsInputView))]
    public sealed class PlayerCombat : MonoBehaviour
    {
        public CombatTuning tuning;
        public GameObject[] weaponModels;
        public CombatFeedback feedback;
        public WeaponDefinition Equipped => tuning != null && tuning.weapons != null && index >= 0 && index < tuning.weapons.Length ? tuning.weapons[index] : null;
        public int ShotsFired { get; private set; }
        public int LastPelletHits { get; private set; }
        private bool[] unlocked;
        private int index;
        private float nextShotAt;
        private FpsInputView view;
        private RigidbodyMotor motor;
        private InputAction attack, next, previous;
        private void Awake() => Initialize();
        public void Initialize()
        {
            if (unlocked != null || tuning == null || tuning.weapons == null || tuning.weapons.Length == 0) return;
            motor = GetComponent<RigidbodyMotor>(); view = GetComponent<FpsInputView>();
            unlocked = new bool[tuning.weapons.Length];
            for (int i = 0; i < unlocked.Length; i++) unlocked[i] = i == 0 || tuning.unlockAll;
            if (view.ActiveActions != null)
            {
                attack = view.ActiveActions.FindAction("Player/Attack");
                next = view.ActiveActions.FindAction("Player/Next");
                previous = view.ActiveActions.FindAction("Player/Previous");
            }
            Equip(0);
        }
        public bool Equip(int slot)
        {
            if (unlocked == null || slot < 0 || slot >= unlocked.Length || !unlocked[slot]) return false;
            index = slot;
            if (weaponModels != null)
                for (int i = 0; i < weaponModels.Length; i++) if (weaponModels[i] != null) weaponModels[i].SetActive(i == index);
            return true;
        }
        public bool Pickup(WeaponDefinition definition)
        {
            Initialize();
            if (unlocked == null) return false;
            int slot = Array.IndexOf(tuning.weapons, definition);
            if (slot < 0) return false;
            unlocked[slot] = true; return Equip(slot);
        }
        private void Cycle(int direction)
        {
            for (int offset = 1; offset <= unlocked.Length; offset++)
                if (Equip((index + direction * offset + unlocked.Length) % unlocked.Length)) return;
        }
        private void Update()
        {
            Initialize();
            if (unlocked == null || !view.InputCaptured || !view.enabled) return;
            var health = GetComponent<CombatHealth>();
            if (health != null && health.IsDead) return;
            if (next?.WasPressedThisFrame() == true) Cycle(1);
            if (previous?.WasPressedThisFrame() == true) Cycle(-1);
            if (Keyboard.current?.digit1Key.wasPressedThisFrame == true) Equip(0);
            if (Keyboard.current?.digit2Key.wasPressedThisFrame == true) Equip(1);
            if (Keyboard.current?.digit3Key.wasPressedThisFrame == true) Equip(2);
            if (attack?.WasPressedThisFrame() == true)
                TryFire(new Ray(view.playerCamera.transform.position, view.playerCamera.transform.forward), Time.time);
        }
        public bool TryFire(Ray aim, float now)
        {
            Initialize();
            var weapon = Equipped;
            if (weapon == null || now < nextShotAt || (TryGetComponent<CombatHealth>(out var health) && health.IsDead)) return false;
            nextShotAt = now + weapon.shotInterval; ShotsFired++;
            if (weapon.kind == WeaponKind.GrenadeLauncher)
            {
                GrenadeProjectile.Spawn(weapon, aim, gameObject, motor.Velocity);
                LastPelletHits = 0;
            }
            else
            {
                var report = WeaponPhysics.Hitscan(weapon, aim, gameObject);
                LastPelletHits = report.DamageHits;
                if (report.DamageHits > 0) ConfirmHit();
            }
            motor.AddVelocityChange(-aim.direction.normalized * weapon.playerRecoil);
            feedback?.Fired(weapon.viewKick);
            if (weapon.kind == WeaponKind.Shotgun && -aim.direction.y * weapon.playerRecoil > 0)
                TimeDirector.Moment(CombatMoment.Boost, motor.Velocity.magnitude + weapon.playerRecoil);
            return true;
        }
        public void ConfirmHit()
        {
            feedback?.Hit();
            if (motor == null) motor = GetComponent<RigidbodyMotor>();
            if (motor.Velocity.magnitude >= tuning.spectacleSpeed)
                TimeDirector.Moment(CombatMoment.TraversalHit, motor.Velocity.magnitude);
        }
    }
}
