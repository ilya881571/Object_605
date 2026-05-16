using UnityEngine;
using UnityEngine.UI; // Не забудьте подключить этот namespace для работы с UI

public class ExitButtonScript : MonoBehaviour
{
    void Start()
    {
        // Если кнопка уже привязана к объекту через Inspector, эта часть не обязательна
        // Здесь можно добавить логику инициализации, если нужно
    }

    public void ExitGame()
    {
        #if UNITY_EDITOR
            // Если игра запущена в редакторе Unity — останавливаем редактор
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            // Если игра запущена вне редактора (на ПК, мобильных устройствах и т. д.) — закрываем приложение
            Application.Quit();
        #endif
    }
}
