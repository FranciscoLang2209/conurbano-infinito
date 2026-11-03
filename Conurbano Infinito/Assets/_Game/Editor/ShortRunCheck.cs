using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class ShortRunCheck
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;

    [MenuItem("Tools/Conurbano Infinito/Comprobar partida corta en Play Mode")]
    public static async void Run()
    {
        if (!Application.isPlaying)
            throw new Exception("Iniciá Play Mode. La prueba reinicia la sesión y termina en Menu.");

        try
        {
            SceneManager.LoadScene("Main");
            await Task.Delay(200);
            GameManager game = Object.FindFirstObjectByType<GameManager>();
            WeaponController weapons = Object.FindFirstObjectByType<WeaponController>();
            ExperienceSystem xp = weapons.GetComponent<ExperienceSystem>();
            UpgradeSelection selection = Object.FindFirstObjectByType<UpgradeSelection>();
            UpgradeData firstWeapon = CharacterData.Selected.FirstWeaponUpgrade;
            Require(game.DurationSeconds == 240f && weapons.EquippedWeaponCount == 1 &&
                xp.Level == 1 && game.State == GameManager.GameState.Playing,
                "La partida no empieza limpia o no dura cuatro minutos.");
            xp.AddExperience(xp.RequiredExperience);
            for (int attempt = 0; attempt < 20; attempt++)
            {
                Require(selection.IsShowing && Time.timeScale == 0f && selection.Options.Count == 3 &&
                    new HashSet<UpgradeData>(selection.Options).Count == 3 &&
                    new List<UpgradeData>(selection.Options).Contains(firstWeapon),
                    "El primer nivel no ofrece tres opciones únicas, incluida el arma del personaje.");
                Invoke(selection, "ShowNext");
            }
            selection.Select(new List<UpgradeData>(selection.Options).IndexOf(firstWeapon));
            Require(weapons.IsEquipped(firstWeapon.TargetWeapon) && !selection.IsShowing && Time.timeScale == 1f,
                "La primera adquisición no equipa el arma o no reanuda el juego.");

            float elapsed = game.ElapsedSeconds;
            game.PauseGame();
            Invoke(game, "Update");
            Require(game.ElapsedSeconds == elapsed && Time.timeScale == 0f,
                "El reloj avanza durante la pausa.");
            game.ContinueGame();
            Require(game.State == GameManager.GameState.Playing && Time.timeScale == 1f,
                "No se reanuda la partida.");

            WaveDirector director = Object.FindFirstObjectByType<WaveDirector>();
            director.Tick(59.99f);
            Require(director.PhaseNumber == 2 && director.ActiveBoss == null, "Fase previa al perro incorrecta.");
            director.Tick(60f);
            Require(director.PhaseNumber == 3 && director.PhaseTitle.Contains("Perros"),
                "Falta la introducción del perro al minuto uno.");
            director.Tick(179.99f);
            Require(director.ActiveBoss == null, "La Jefa aparece antes del minuto tres.");
            director.Tick(180f);
            Health boss = director.ActiveBoss;
            director.Tick(180f);
            Require(boss != null && director.ActiveBoss == boss && director.PhaseNumber == 5 &&
                Object.FindObjectsByType<BossAttack>(FindObjectsSortMode.None).Length == 1,
                "La Jefa no aparece una sola vez al minuto tres.");
            BossAttack attack = boss.GetComponent<BossAttack>();
            typeof(BossAttack).GetField("nextAttackTime", Flags).SetValue(attack, 0f);
            game.PauseGame();
            Invoke(attack, "Update");
            Require(GameObject.Find("Aviso de ataque de La Jefa") == null,
                "La Jefa prepara un ataque durante la pausa.");
            game.ContinueGame();
            Invoke(attack, "Update");
            Require(GameObject.Find("Aviso de ataque de La Jefa") != null &&
                new SerializedObject(Object.FindFirstObjectByType<AudioDirector>())
                    .FindProperty("attackWarning").objectReferenceValue != null,
                "Falta el aviso visual o el clip de aviso conectado.");
            boss.TakeDamage(boss.MaximumHealth);
            Invoke(attack, "Update");
            await Task.Delay(100);
            Require(GameObject.Find("Aviso de ataque de La Jefa") == null,
                "La Jefa conserva su ataque después de morir.");
            director.Tick(210f);
            Require(director.PhaseNumber == 6, "No empieza el cierre a los 3:30.");

            typeof(GameManager).GetField("<ElapsedSeconds>k__BackingField", Flags).SetValue(game, 240f);
            Invoke(game, "Update");
            Require(game.State == GameManager.GameState.Victory && Time.timeScale == 0f &&
                !weapons.enabled && !director.enabled, "La victoria no detiene el combate a los 4:00.");
            game.RestartGame();
            await Task.Delay(200);
            game = Object.FindFirstObjectByType<GameManager>();
            weapons = Object.FindFirstObjectByType<WeaponController>();
            Health health = weapons.GetComponent<Health>();
            Require(game.ElapsedSeconds < 2f && game.BossHealth == null && weapons.EquippedWeaponCount == 1 &&
                health.CurrentHealth == health.MaximumHealth && weapons.GetComponent<ExperienceSystem>().Level == 1,
                "El reinicio conserva tiempo, jefe, armas, experiencia o daño.");
            health.TakeDamage(health.MaximumHealth);
            Require(game.State == GameManager.GameState.Defeat && Time.timeScale == 0f && !weapons.enabled,
                "La muerte no termina la partida.");
            game.ReturnToMenu();
            await Task.Delay(200);
            Require(SceneManager.GetActiveScene().name == "Menu" &&
                Object.FindFirstObjectByType<MenuScreen>() != null && Time.timeScale == 1f,
                "No se puede volver al menú desde derrota.");
            Debug.Log("SHORT_RUN_CHECK_PASS: primera elección, pausa, seis fases, jefe único, victoria, reinicio limpio, derrota y menú.");
        }
        catch (Exception error)
        {
            Debug.LogException(error);
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    private static void Invoke(object target, string method) =>
        target.GetType().GetMethod(method, Flags).Invoke(target, null);
}
