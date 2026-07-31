using System;
using System.Collections.Generic;
using System.IO;

namespace NiposChecker.Localization;

public static class Strings
{
	private const string DefaultLang = "RU";

	public static readonly string[] Supported = new string[2] { "RU", "EN" };

	private static readonly Dictionary<string, string> RU;

	private static readonly Dictionary<string, string> EN;

	private static string LangFilePath => Path.Combine(AppContext.BaseDirectory, "lang.cfg");

	public static string Current { get; private set; } = LoadSaved();

	public static event Action LanguageChanged;

	private static string LoadSaved()
	{
		try
		{
			string path = Path.Combine(AppContext.BaseDirectory, "lang.cfg");
			if (File.Exists(path))
			{
				string text = File.ReadAllText(path).Trim().ToUpperInvariant();
				if (text == "RU" || text == "EN")
				{
					return text;
				}
			}
		}
		catch
		{
		}
		return "RU";
	}

	private static void Save()
	{
		try
		{
			File.WriteAllText(LangFilePath, Current);
		}
		catch
		{
		}
	}

	public static void SetLanguage(string lang)
	{
		if (!string.IsNullOrWhiteSpace(lang))
		{
			lang = lang.Trim().ToUpperInvariant();
			if ((!(lang != "RU") || !(lang != "EN")) && !(lang == Current))
			{
				Current = lang;
				Save();
				Strings.LanguageChanged?.Invoke();
			}
		}
	}

	public static void Toggle()
	{
		SetLanguage((Current == "RU") ? "EN" : "RU");
	}

	public static string Get(string key)
	{
		if (!((Current == "EN") ? EN : RU).TryGetValue(key, out var value))
		{
			return key;
		}
		return value;
	}

	public static string Get(string key, params object[] args)
	{
		string text = Get(key);
		try
		{
			return string.Format(text, args);
		}
		catch
		{
			return text;
		}
	}

	static Strings()
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		dictionary["Menu_Search"] = "Поиск файлов";
		dictionary["Menu_USB"] = "USB Устройства";
		dictionary["Menu_LastActivity"] = "Последняя активность";
		dictionary["Menu_Software"] = "Программы";
		dictionary["Menu_Registry"] = "Реестр ПК";
		dictionary["Menu_Other"] = "Другое";
		dictionary["Menu_Steam"] = "STEAM Аккаунты";
		dictionary["Menu_Traces"] = "Следы / Антиобход";
		dictionary["Tab_Search"] = "Поиск файлов";
		dictionary["Tab_USB"] = "USB Устройства";
		dictionary["Tab_LastActivity"] = "Последняя активность";
		dictionary["Tab_Software"] = "Программы";
		dictionary["Tab_Registry"] = "Реестр ПК";
		dictionary["Tab_Other"] = "Другое";
		dictionary["Tab_Steam"] = "STEAM Аккаунты";
		dictionary["Tab_Traces"] = "Следы / Антиобход";
		dictionary["Traces_Checking"] = "Идёт проверка следов…";
		dictionary["Traces_Check"] = "Проверить";
		dictionary["Traces_Recheck"] = "Обновить";
		dictionary["Traces_None"] = "Сигналов не найдено";
		dictionary["Traces_FoundTitle"] = "Есть признаки уборки следов";
		dictionary["Traces_FoundSub"] = "Тревог: {0} · внимание: {1} — проверьте вручную";
		dictionary["Traces_WarnTitle"] = "Есть, на что обратить внимание";
		dictionary["Traces_WarnSub"] = "Внимание: {0} — возможно, ложные срабатывания";
		dictionary["Traces_CleanTitle"] = "Явных следов уборки не найдено";
		dictionary["Traces_CleanSub"] = "Подозрительных сигналов нет";
		dictionary["Traces_RunAsAdmin"] = "Запустить от админа";
		dictionary["Traces_DetailsHint"] = "Двойной клик — открыть папку с файлом";
		dictionary["Traces_ColLastRun"] = "Последний запуск";
		dictionary["Menu_Processes"] = "Процессы / Инъекции";
		dictionary["Tab_Processes"] = "Процессы / Инъекции";
		dictionary["Proc_Scan"] = "Сканировать";
		dictionary["Proc_Rescan"] = "Пересканировать";
		dictionary["Proc_Scanning"] = "Идёт скан процессов…";
		dictionary["Proc_None"] = "Подозрительных процессов не найдено";
		dictionary["Proc_ColName"] = "Процесс / модуль";
		dictionary["Proc_ColNote"] = "Замечание";
		dictionary["Proc_FoundTitle"] = "Найдены признаки чита в памяти";
		dictionary["Proc_FoundSub"] = "Чит/инъекций: {0} · внимание: {1} · всего процессов: {2}";
		dictionary["Proc_WarnTitle"] = "Есть, на что обратить внимание";
		dictionary["Proc_WarnSub"] = "Подозрительных: {0} · всего процессов: {1}";
		dictionary["Proc_CleanTitle"] = "Подозрительных процессов не найдено";
		dictionary["Proc_CleanSub"] = "Проверено процессов: {0}";
		dictionary["Report_Tooltip"] = "Отправить отчёт админу";
		dictionary["Report_Title"] = "Отчёт проверки";
		dictionary["Report_Sent"] = "Отчёт отправлен администратору.";
		dictionary["Report_Fail"] = "Не удалось отправить отчёт. Проверьте интернет и попробуйте снова.";
		dictionary["Btn_Folders"] = "Папки";
		dictionary["Btn_Search"] = "Поиск";
		dictionary["Btn_Stop"] = "■ Стоп";
		dictionary["Btn_SelectFolders"] = "Выбрать папки";
		dictionary["Btn_StartSearch"] = "Начать поиск";
		dictionary["Search_Found"] = "Найдено: 0";
		dictionary["Search_FoundFmt"] = "Найдено: {0}";
		dictionary["Col_Name"] = "Имя";
		dictionary["Col_Cheat"] = "Чит";
		dictionary["Col_Type"] = "Тип";
		dictionary["Col_Size"] = "Размер";
		dictionary["Col_Modified"] = "Изменён";
		dictionary["Col_Access"] = "Доступ";
		dictionary["Col_Path"] = "Путь";
		dictionary["Col_Risk"] = "Риск";
		dictionary["Msg_NoFolders"] = "Не выбраны папки.";
		dictionary["Msg_SearchRunning"] = "Поиск уже запущен.";
		dictionary["Title_Search"] = "Поиск";
		dictionary["SO_WindowTitle"] = "Выбор папок";
		dictionary["SO_Title"] = "Режим поиска";
		dictionary["SO_Fast"] = "⚡ Быстрый поиск (Downloads, Desktop, Documents, AppData)";
		dictionary["SO_SystemDrive"] = "\ud83d\udcbb Системный диск (полный)";
		dictionary["SO_ManualDrives"] = "\ud83d\udcc1 Выбор дисков вручную";
		dictionary["SO_BrowseFolder"] = "\ud83d\udcc2 Выбрать папку...";
		dictionary["SO_Methods"] = "Методы проверки:";
		dictionary["SO_ByIcon"] = "По иконке (медленнее)";
		dictionary["SO_BySignature"] = "По цифровой подписи";
		dictionary["SO_SelectedFolders"] = "Выбранные папки:";
		dictionary["SO_SelectDrives"] = "Выберите диски:";
		dictionary["SO_SelectedCount"] = "Выбрано папок: {0}";
		dictionary["Steam_CurrentAccount"] = "Текущий STEAM аккаунт";
		dictionary["Steam_OtherAccounts"] = "Другие STEAM аккаунты";
		dictionary["Btn_ExportAccounts"] = "Экспорт аккаунтов";
		dictionary["Btn_CheckBlockDB"] = "Проверка аккаунтов через BlockDB";
		dictionary["Btn_CheckBlockIP"] = "Проверка IP через BlockDB";
		dictionary["Steam_ProfileType"] = "Тип профиля : {0}";
		dictionary["Steam_VacYes"] = "Блокировка VAC : Да ({0} дн. назад)";
		dictionary["Steam_VacNo"] = "Блокировка VAC : Нет";
		dictionary["Steam_RealName"] = "Настоящее имя : {0}";
		dictionary["Steam_RegDate"] = "Дата регистрации : {0}";
		dictionary["Steam_BuyDate"] = "Дата покупки игры : {0}";
		dictionary["Steam_Ip"] = "IP: {0}";
		dictionary["Steam_LastActivity"] = "Последняя активность: {0}";
		dictionary["Msg_NoAccounts"] = "Нет аккаунтов.";
		dictionary["Msg_NoIp"] = "IP не определён.";
		dictionary["Msg_Exported"] = "Экспортировано {0} аккаунтов:\n{1}";
		dictionary["Title_Export"] = "Экспорт";
		dictionary["Word_Yes"] = "Да";
		dictionary["Word_No"] = "Нет";
		dictionary["Word_Unknown"] = "Неизвестно";
		dictionary["Cat_FileAnalysis"] = "Анализ файлов / Директорий ПК";
		dictionary["Cat_BrowserAnalysis"] = "Анализ браузеров / Веб-приложений";
		dictionary["Cat_GameAnalysis"] = "Анализ процесса игры";
		dictionary["Cat_RegistryAnalysis"] = "Анализ реестра";
		dictionary["Cat_SystemApps"] = "Открыть системные приложения";
		dictionary["Btn_DataUsage"] = "Использование данных";
		dictionary["Btn_Nvidia"] = "Панель управления NVIDIA";
		dictionary["Btn_Services"] = "Службы Windows";
		dictionary["Cat_KeyEmulation"] = "Эмуляция нажатия клавиш в игре";
		dictionary["Btn_AutoCheck"] = "Автоматическая проверка в игре";
		dictionary["Game_NotRunning"] = "Процесс игры: Игра не запущена!";
		dictionary["Game_Running"] = "Процесс игры: запущена ({0})";
		dictionary["Btn_Keyboard"] = "Открыть экранную клавиатуру";
		dictionary["Cat_Macros"] = "Проверка на макросы";
		dictionary["Cat_MacrosHint"] = "Ручная и автоматическая проверка";
		dictionary["Btn_MouseApp"] = "Программа управления мышью";
		dictionary["Btn_MacroCheck"] = "Проверка на запущенный макрос";
		dictionary["Sys_Windows"] = "Система: {0}";
		dictionary["Sys_InstallDate"] = "Дата установки системы: {0}";
		dictionary["Sys_Uptime"] = "Время запуска сеанса: {0}";
		dictionary["Sys_Ram"] = "Объем ОЗУ: {0}";
		dictionary["Sys_Cpu"] = "Процессор: {0}";
		dictionary["Sys_Gpu"] = "Видеокарта: {0}";
		dictionary["Sys_Motherboard"] = "Материнская плата: {0}";
		dictionary["Sys_Screens"] = "Количество экранов: {0}";
		dictionary["Sys_VmYes"] = "Система работает в виртуальной машине: Да ({0})";
		dictionary["Sys_VmNo"] = "Система работает в виртуальной машине: Нет";
		dictionary["Btn_LoadUSB"] = "Загрузить USB";
		dictionary["Status_Loading"] = "Загрузка...";
		dictionary["Status_UsbStart"] = "Запуск USBDeview.exe...";
		dictionary["Col_Device"] = "Устройство";
		dictionary["Col_DevType"] = "Тип";
		dictionary["Col_Letter"] = "Буква";
		dictionary["Col_Connected"] = "Подключено";
		dictionary["Col_Disconnected"] = "Откл.";
		dictionary["Btn_Load"] = "Загрузить";
		dictionary["Btn_Reset"] = "Сброс";
		dictionary["Col_FileName"] = "Имя файла";
		dictionary["Col_Date"] = "Дата";
		dictionary["Status_LoadingActivity"] = "Загрузка активности...";
		dictionary["Btn_StartAnalysis"] = "Начать анализ";
		dictionary["Col_Application"] = "Приложение";
		dictionary["Notif_Title"] = "Автодетект читов";
		dictionary["Notif_NothingFound"] = "Ничего не обнаружено ✓";
		dictionary["Tip_Sound"] = "Звук интерфейса вкл/выкл";
		dictionary["Tip_Notifications"] = "Уведомления";
		dictionary["Tip_Settings"] = "Настройки";
		dictionary["Tip_Language"] = "Язык";
		dictionary["Load_GettingInfo"] = "Получение информации с сервера...";
		dictionary["Load_CheatDB"] = "Загрузка базы читов...";
		dictionary["Load_SteamInit"] = "Инициализация Steam...";
		dictionary["Load_SteamAccounts"] = "Поиск Steam-аккаунтов...";
		dictionary["Load_Profiles"] = "Загрузка профилей...";
		dictionary["Load_HWID"] = "Проверка аппаратного ID...";
		dictionary["Load_Done"] = "Готово ✓";
		dictionary["Load_Error"] = "Ошибка инициализации";
		dictionary["Load_Blocked"] = "Блокировка";
		dictionary["Update_Available"] = "Доступна новая версия: {0}\nТекущая версия: {1}\n\nПерейти на сайт для обновления?";
		dictionary["BlockDb_TitleSteam"] = "Проверка аккаунтов через BlockDB";
		dictionary["BlockDb_TitleIp"] = "Проверка IP через BlockDB";
		dictionary["BlockDb_Checking"] = "Проверка: {0}";
		dictionary["BlockDb_Progress"] = "Проверено {0} из {1}";
		dictionary["BlockDb_NoBans"] = "Блокировок не найдено ✓";
		dictionary["BlockDb_Found"] = "Найдено блокировок: {0}";
		dictionary["BlockDb_ColId"] = "ID";
		dictionary["BlockDb_ColSteamId"] = "SteamID";
		dictionary["BlockDb_ColIp"] = "IP";
		dictionary["BlockDb_ColReason"] = "Причина";
		dictionary["BlockDb_ColProject"] = "Проект";
		dictionary["BlockDb_ColDuration"] = "Срок";
		dictionary["BlockDb_ColDate"] = "Дата";
		dictionary["BlockDb_Error"] = "Ошибка запроса к BlockDB: {0}";
		dictionary["Banned_Title"] = "Доступ заблокирован";
		dictionary["Banned_Reason"] = "Причина: {0}";
		dictionary["Banned_Close"] = "Закрыть";
		dictionary["Macros_Title"] = "Проверка на макросы";
		dictionary["Macros_Start"] = "Начать";
		dictionary["Macros_Stop"] = "Остановить";
		dictionary["Macros_Clear"] = "Очистить";
		dictionary["Macros_Close"] = "Закрыть";
		dictionary["Macros_Hint"] = "Зажмите ЛКМ и ведите мышь по полю";
		dictionary["Emulate_Title"] = "Автоматическая проверка в игре";
		dictionary["Emulate_Start"] = "Запустить";
		dictionary["Emulate_Stop"] = "Остановить";
		dictionary["Emulate_Close"] = "Закрыть";
		dictionary["Btn_Ok"] = "OK";
		dictionary["Btn_Cancel"] = "Отмена";
		dictionary["Btn_Close"] = "Закрыть";
		dictionary["Title_Error"] = "Ошибка";
		dictionary["Title_Info"] = "Информация";
		dictionary["Msg_Error"] = "Ошибка: {0}";
		dictionary["Msg_InitError"] = "Ошибка инициализации: {0}";
		dictionary["Msg_ExitConfirm"] = "Вы уверены, что хотите закрыть чекер?";
		dictionary["Title_Exit"] = "Выход";
		dictionary["Exit_YesBtn"] = "Да, выйти";
		dictionary["Btn_Yes"] = "Да";
		dictionary["Btn_No"] = "Нет";
		dictionary["Btn_Ok"] = "OK";
		dictionary["Msg_Cancel"] = "Отмена";
		dictionary["Title_BlockDb"] = "BlockDB";
		dictionary["Msg_NvidiaNotFound"] = "NVIDIA Control Panel не найден.";
		dictionary["Title_Nvidia"] = "NVIDIA";
		dictionary["Mouse_Detected"] = "Обнаружена мышь: {0}";
		dictionary["Mouse_Standard"] = "Мышь: стандартная (без ПО управления)";
		dictionary["Mouse_Error"] = "Ошибка детекта мыши.";
		dictionary["Title_MouseDetect"] = "Детект мыши";
		dictionary["Banned_WindowTitle"] = "Блокировка";
		dictionary["Banned_Header"] = "⛔ ВАШЕ УСТРОЙСТВО ЗАБЛОКИРОВАНО";
		dictionary["Banned_ReasonUnknown"] = "Причина не указана";
		dictionary["Banned_Date"] = "Дата блокировки: {0}";
		dictionary["Banned_EndDate"] = "Дата окончания: {0}";
		dictionary["Banned_IssuedBy"] = "Выдал: {0}";
		dictionary["Banned_CanClose"] = "Вы можете закрыть это окно.";
		dictionary["Banned_Countdown"] = "Окно закроется через {0}...";
		dictionary["BlockDb_WindowTitle"] = "BlockDB — проверка банов";
		dictionary["BlockDb_Accounts"] = "АККАУНТЫ";
		dictionary["BlockDb_Waiting"] = "Ожидание...";
		dictionary["BlockDb_NoBansShort"] = "✅ Банов не найдено";
		dictionary["BlockDb_Details"] = "ДЕТАЛИ БАНА";
		dictionary["BlockDb_ColStatus"] = "Статус / Истекает";
		dictionary["BlockDb_ColIssued"] = "Выдан";
		dictionary["BlockDb_CloseBtn"] = "Закрыть";
		dictionary["BlockDb_CheckingAccounts"] = "Проверка аккаунтов...";
		dictionary["BlockDb_Done"] = "Готово";
		dictionary["BlockDb_BansCount"] = "Банов: {0}";
		dictionary["BlockDb_ProjectBan"] = "Бан проекта: {0}";
		dictionary["BlockDb_HistoryOnly"] = "История: {0}, нет бана проекта";
		dictionary["BlockDb_Clean"] = "Чист";
		dictionary["BlockDb_ErrorShort"] = "";
		dictionary["BlockDb_CheckingIp"] = "Проверка IP: {0}";
		dictionary["BlockDb_IpRequest"] = "IP-запрос";
		dictionary["BlockDb_DetailsFor"] = "ДЕТАЛИ БАНА — {0}";
		dictionary["SO_StatusFast"] = "✔ Быстрый поиск";
		dictionary["SO_StatusSystem"] = "✔ Системный диск";
		dictionary["SO_StatusDrives"] = "✔ Выберите диски";
		dictionary["SO_StatusFolder"] = "✔ Папка выбрана";
		dictionary["SO_DriveScanError"] = "Ошибка сканирования диска:\n{0}";
		dictionary["SO_NoFolders"] = "Не выбрано ни одной папки для сканирования.";
		dictionary["SO_PickFolder"] = "Выберите папку для сканирования";
		dictionary["Emulate_WindowTitle"] = "Проверка в игре";
		dictionary["Emulate_Desc"] = "Нажимает все клавиши в окне CS2.\nЕсли макрос привязан к клавише — он сработает.";
		dictionary["Emulate_StartCheck"] = "Начать проверку";
		dictionary["Emulate_GameNotRunning"] = "CS2 не запущена! Запустите игру и попробуйте снова.";
		dictionary["Emulate_CheckTitle"] = "Проверка";
		dictionary["Emulate_Instructions"] = "Чтобы проверка работала корректно, в настройках CS2 поставьте:\n\n• Режим отображения: В ОКНЕ или ПОЛНОЭКРАННЫЙ В ОКНЕ\n• Поставьте это окно НАД окном игры\n\nПосле этого нажмите [ОК]";
		dictionary["Emulate_Done"] = "✓ Проверка завершена!";
		dictionary["Macros_Color"] = "Цвет:";
		dictionary["Macros_CrosshairCS2"] = "Прицел CS2";
		dictionary["Macros_CrosshairModel"] = "Прицел модели";
		dictionary["Msg_AlreadyRunning"] = "Приложение уже запущено.";
		dictionary["Msg_SteamNotRunning"] = "Steam не запущен! Запустите Steam и повторите попытку.";
		dictionary["Profile_Hidden"] = "Скрытый";
		dictionary["Profile_Open"] = "Открытый";
		dictionary["Profile_Friends"] = "Только для друзей";
		dictionary["Vac_YesDays"] = "VAC: да ({0} дн.)";
		dictionary["Vac_Yes"] = "VAC: да";
		dictionary["Ban_Permanent"] = "♾ Перманент";
		dictionary["Ban_LiftedPerm"] = "✅ Снят (перм.)";
		dictionary["Ban_ActiveUntil"] = "\ud83d\udd34 Активен до {0:dd.MM.yyyy HH:mm}";
		dictionary["Ban_Lifted"] = "✅ Снят {0:dd.MM.yyyy HH:mm}";
		dictionary["Ban_Forever"] = "Навсегда";
		dictionary["Ban_Minutes"] = "{0} мин";
		dictionary["AD_Days"] = "{0} дн.";
		dictionary["AD_Hours"] = "{0} ч.";
		dictionary["AD_Minutes"] = "{0} мин.";
		dictionary["AD_CleanDetected"] = "Обнаружена очистка";
		dictionary["AD_ServiceOff"] = "Служба отключена";
		dictionary["AD_XoneRunning"] = "XONE ЗАПУЩЕН";
		dictionary["AD_FirewallSusp"] = "Подозрительный брандмауэр";
		dictionary["AD_ExLoader"] = "ExLoader: файл strings.txt ({0} назад)";
		dictionary["AD_DusmStopped"] = "DusmSvc (Использование данных) остановлена";
		dictionary["AD_Process"] = "Процесс: {0}";
		dictionary["AD_FirewallFew"] = "Мало правил inbound ({0}) — возможна очистка";
		dictionary["Msg_NoInternet"] = "Нет подключения к интернету!";
		dictionary["Search_ExportTitle"] = "Отчет поиска файлов";
		dictionary["Search_ExportCount"] = "Количество обнаруженных файлов: {0}";
		dictionary["Steam_ProjectBan"] = "Бан на проекте : {0}";
		dictionary["Steam_ProjectBanBadge"] = "БАН ПРОЕКТА";
		dictionary["Lang_WindowTitle"] = "Выбор языка";
		dictionary["Lang_Russian"] = "Русский";
		dictionary["Lang_English"] = "English";
		dictionary["Settings_Sound"] = "Звук интерфейса";
		dictionary["Settings_Volume"] = "Громкость: {0}%";
		dictionary["AD_StartService"] = "Запустить службу";
		dictionary["AD_ServiceStarted"] = "Служба запущена ✓";
		dictionary["AD_ServiceStartFail"] = "Не удалось запустить службу";
		dictionary["Mouse_OpenApp"] = "Открыть {0}";
		dictionary["Mouse_NoApp"] = "ПО управления мышью не найдено (Logitech G HUB).";
		dictionary["Act_Regex"] = "Regex";
		dictionary["Act_LoadExecuted"] = "Загрузить ExecutedProgramsList";
		dictionary["DataUsage_StartPrompt"] = "Служба DusmSvc (Использование данных) остановлена.\nЗапустить её?";
		dictionary["DataUsage_Title"] = "Использование данных";
		dictionary["Discord_Checking"] = "Проверяет ПК на запрещённое ПО";
		dictionary["Discord_OpenSite"] = "Открыть сайт";
		dictionary["Link_Website"] = "Веб-сайт";
		dictionary["Link_Discord"] = "Discord";
		dictionary["Link_VK"] = "VK";
		dictionary["FL_ChooseLanguage"] = "Выберите язык";
		dictionary["FL_ChooseSettings"] = "Настройки";
		dictionary["FL_Discord"] = "Discord-статус (проверяет ПК)";
		dictionary["FL_SaveAndStart"] = "Сохранить и начать";
		dictionary["Btn_Next"] = "Далее";
		dictionary["Btn_Back"] = "Назад";
		RU = dictionary;
		Dictionary<string, string> dictionary2 = new Dictionary<string, string>();
		dictionary2["Menu_Search"] = "File Search";
		dictionary2["Menu_USB"] = "USB Devices";
		dictionary2["Menu_LastActivity"] = "Last Activity";
		dictionary2["Menu_Software"] = "Software";
		dictionary2["Menu_Registry"] = "PC Registry";
		dictionary2["Menu_Other"] = "Other";
		dictionary2["Menu_Steam"] = "STEAM Accounts";
		dictionary2["Menu_Traces"] = "Traces / Anti-evasion";
		dictionary2["Tab_Search"] = "File Search";
		dictionary2["Tab_USB"] = "USB Devices";
		dictionary2["Tab_LastActivity"] = "Last Activity";
		dictionary2["Tab_Software"] = "Software";
		dictionary2["Tab_Registry"] = "PC Registry";
		dictionary2["Tab_Other"] = "Other";
		dictionary2["Tab_Steam"] = "STEAM Accounts";
		dictionary2["Tab_Traces"] = "Traces / Anti-evasion";
		dictionary2["Traces_Checking"] = "Checking traces…";
		dictionary2["Traces_Check"] = "Check";
		dictionary2["Traces_Recheck"] = "Refresh";
		dictionary2["Traces_None"] = "No signals found";
		dictionary2["Traces_FoundTitle"] = "Signs of trace cleanup detected";
		dictionary2["Traces_FoundSub"] = "Alerts: {0} · warnings: {1} — verify manually";
		dictionary2["Traces_WarnTitle"] = "Some things to review";
		dictionary2["Traces_WarnSub"] = "Warnings: {0} — possibly false positives";
		dictionary2["Traces_CleanTitle"] = "No obvious cleanup traces";
		dictionary2["Traces_CleanSub"] = "No suspicious signals";
		dictionary2["Traces_RunAsAdmin"] = "Run as admin";
		dictionary2["Traces_DetailsHint"] = "Double-click to open the file's folder";
		dictionary2["Traces_ColLastRun"] = "Last run";
		dictionary2["Menu_Processes"] = "Processes / Injections";
		dictionary2["Tab_Processes"] = "Processes / Injections";
		dictionary2["Proc_Scan"] = "Scan";
		dictionary2["Proc_Rescan"] = "Rescan";
		dictionary2["Proc_Scanning"] = "Scanning processes…";
		dictionary2["Proc_None"] = "No suspicious processes found";
		dictionary2["Proc_ColName"] = "Process / module";
		dictionary2["Proc_ColNote"] = "Note";
		dictionary2["Proc_FoundTitle"] = "Signs of an in-memory cheat found";
		dictionary2["Proc_FoundSub"] = "Cheat/injections: {0} · warnings: {1} · total processes: {2}";
		dictionary2["Proc_WarnTitle"] = "Some things to review";
		dictionary2["Proc_WarnSub"] = "Suspicious: {0} · total processes: {1}";
		dictionary2["Proc_CleanTitle"] = "No suspicious processes found";
		dictionary2["Proc_CleanSub"] = "Processes checked: {0}";
		dictionary2["Report_Tooltip"] = "Send report to admin";
		dictionary2["Report_Title"] = "Check report";
		dictionary2["Report_Sent"] = "Report sent to the administrator.";
		dictionary2["Report_Fail"] = "Failed to send the report. Check your internet and try again.";
		dictionary2["Btn_Folders"] = "Folders";
		dictionary2["Btn_Search"] = "Search";
		dictionary2["Btn_Stop"] = "■ Stop";
		dictionary2["Btn_SelectFolders"] = "Select folders";
		dictionary2["Btn_StartSearch"] = "Start search";
		dictionary2["Search_Found"] = "Found: 0";
		dictionary2["Search_FoundFmt"] = "Found: {0}";
		dictionary2["Col_Name"] = "Name";
		dictionary2["Col_Cheat"] = "Cheat";
		dictionary2["Col_Type"] = "Type";
		dictionary2["Col_Size"] = "Size";
		dictionary2["Col_Modified"] = "Modified";
		dictionary2["Col_Access"] = "Access";
		dictionary2["Col_Path"] = "Path";
		dictionary2["Col_Risk"] = "Risk";
		dictionary2["Msg_NoFolders"] = "No folders selected.";
		dictionary2["Msg_SearchRunning"] = "Search is already running.";
		dictionary2["Title_Search"] = "Search";
		dictionary2["SO_WindowTitle"] = "Select folders";
		dictionary2["SO_Title"] = "Search mode";
		dictionary2["SO_Fast"] = "⚡ Quick scan (Downloads, Desktop, Documents, AppData)";
		dictionary2["SO_SystemDrive"] = "\ud83d\udcbb System drive (full)";
		dictionary2["SO_ManualDrives"] = "\ud83d\udcc1 Pick drives manually";
		dictionary2["SO_BrowseFolder"] = "\ud83d\udcc2 Browse for folder...";
		dictionary2["SO_Methods"] = "Detection methods:";
		dictionary2["SO_ByIcon"] = "By icon (slower)";
		dictionary2["SO_BySignature"] = "By digital signature";
		dictionary2["SO_SelectedFolders"] = "Selected folders:";
		dictionary2["SO_SelectDrives"] = "Select drives:";
		dictionary2["SO_SelectedCount"] = "Folders selected: {0}";
		dictionary2["Steam_CurrentAccount"] = "Current STEAM account";
		dictionary2["Steam_OtherAccounts"] = "Other STEAM accounts";
		dictionary2["Btn_ExportAccounts"] = "Export accounts";
		dictionary2["Btn_CheckBlockDB"] = "Check accounts via BlockDB";
		dictionary2["Btn_CheckBlockIP"] = "Check IP via BlockDB";
		dictionary2["Steam_ProfileType"] = "Profile type : {0}";
		dictionary2["Steam_VacYes"] = "VAC ban : Yes ({0} days ago)";
		dictionary2["Steam_VacNo"] = "VAC ban : No";
		dictionary2["Steam_RealName"] = "Real name : {0}";
		dictionary2["Steam_RegDate"] = "Registration date : {0}";
		dictionary2["Steam_BuyDate"] = "Game purchase date : {0}";
		dictionary2["Steam_Ip"] = "IP: {0}";
		dictionary2["Steam_LastActivity"] = "Last activity: {0}";
		dictionary2["Msg_NoAccounts"] = "No accounts found.";
		dictionary2["Msg_NoIp"] = "IP could not be determined.";
		dictionary2["Msg_Exported"] = "Exported {0} accounts:\n{1}";
		dictionary2["Title_Export"] = "Export";
		dictionary2["Word_Yes"] = "Yes";
		dictionary2["Word_No"] = "No";
		dictionary2["Word_Unknown"] = "Unknown";
		dictionary2["Cat_FileAnalysis"] = "File / Directory Analysis";
		dictionary2["Cat_BrowserAnalysis"] = "Browser / Web App Analysis";
		dictionary2["Cat_GameAnalysis"] = "Game Process Analysis";
		dictionary2["Cat_RegistryAnalysis"] = "Registry Analysis";
		dictionary2["Cat_SystemApps"] = "Open System Applications";
		dictionary2["Btn_DataUsage"] = "Data Usage";
		dictionary2["Btn_Nvidia"] = "NVIDIA Control Panel";
		dictionary2["Btn_Services"] = "Windows Services";
		dictionary2["Cat_KeyEmulation"] = "In-game Key Emulation";
		dictionary2["Btn_AutoCheck"] = "Automatic in-game check";
		dictionary2["Game_NotRunning"] = "Game process: Game not running!";
		dictionary2["Game_Running"] = "Game process: running ({0})";
		dictionary2["Btn_Keyboard"] = "Open on-screen keyboard";
		dictionary2["Cat_Macros"] = "Macro Check";
		dictionary2["Cat_MacrosHint"] = "Manual and automatic check";
		dictionary2["Btn_MouseApp"] = "Mouse control software";
		dictionary2["Btn_MacroCheck"] = "Check for running macro";
		dictionary2["Sys_Windows"] = "System: {0}";
		dictionary2["Sys_InstallDate"] = "System install date: {0}";
		dictionary2["Sys_Uptime"] = "Session start time: {0}";
		dictionary2["Sys_Ram"] = "RAM: {0}";
		dictionary2["Sys_Cpu"] = "Processor: {0}";
		dictionary2["Sys_Gpu"] = "Graphics card: {0}";
		dictionary2["Sys_Motherboard"] = "Motherboard: {0}";
		dictionary2["Sys_Screens"] = "Number of displays: {0}";
		dictionary2["Sys_VmYes"] = "Running in a virtual machine: Yes ({0})";
		dictionary2["Sys_VmNo"] = "Running in a virtual machine: No";
		dictionary2["Btn_LoadUSB"] = "Load USB";
		dictionary2["Status_Loading"] = "Loading...";
		dictionary2["Status_UsbStart"] = "Starting USBDeview.exe...";
		dictionary2["Col_Device"] = "Device";
		dictionary2["Col_DevType"] = "Type";
		dictionary2["Col_Letter"] = "Drive";
		dictionary2["Col_Connected"] = "Connected";
		dictionary2["Col_Disconnected"] = "Disconn.";
		dictionary2["Btn_Load"] = "Load";
		dictionary2["Btn_Reset"] = "Reset";
		dictionary2["Col_FileName"] = "File name";
		dictionary2["Col_Date"] = "Date";
		dictionary2["Status_LoadingActivity"] = "Loading activity...";
		dictionary2["Btn_StartAnalysis"] = "Start analysis";
		dictionary2["Col_Application"] = "Application";
		dictionary2["Notif_Title"] = "Auto-detect cheats";
		dictionary2["Notif_NothingFound"] = "Nothing detected ✓";
		dictionary2["Tip_Sound"] = "Interface sound on/off";
		dictionary2["Tip_Notifications"] = "Notifications";
		dictionary2["Tip_Settings"] = "Settings";
		dictionary2["Tip_Language"] = "Language";
		dictionary2["Load_GettingInfo"] = "Getting server information...";
		dictionary2["Load_CheatDB"] = "Loading cheat database...";
		dictionary2["Load_SteamInit"] = "Initializing Steam...";
		dictionary2["Load_SteamAccounts"] = "Searching Steam accounts...";
		dictionary2["Load_Profiles"] = "Loading profiles...";
		dictionary2["Load_HWID"] = "Checking hardware ID...";
		dictionary2["Load_Done"] = "Done ✓";
		dictionary2["Load_Error"] = "Initialization error";
		dictionary2["Load_Blocked"] = "Blocked";
		dictionary2["Update_Available"] = "New version available: {0}\nCurrent version: {1}\n\nGo to the website to update?";
		dictionary2["BlockDb_TitleSteam"] = "Checking accounts via BlockDB";
		dictionary2["BlockDb_TitleIp"] = "Checking IP via BlockDB";
		dictionary2["BlockDb_Checking"] = "Checking: {0}";
		dictionary2["BlockDb_Progress"] = "Checked {0} of {1}";
		dictionary2["BlockDb_NoBans"] = "No bans found ✓";
		dictionary2["BlockDb_Found"] = "Bans found: {0}";
		dictionary2["BlockDb_ColId"] = "ID";
		dictionary2["BlockDb_ColSteamId"] = "SteamID";
		dictionary2["BlockDb_ColIp"] = "IP";
		dictionary2["BlockDb_ColReason"] = "Reason";
		dictionary2["BlockDb_ColProject"] = "Project";
		dictionary2["BlockDb_ColDuration"] = "Duration";
		dictionary2["BlockDb_ColDate"] = "Date";
		dictionary2["BlockDb_Error"] = "BlockDB request error: {0}";
		dictionary2["Banned_Title"] = "Access blocked";
		dictionary2["Banned_Reason"] = "Reason: {0}";
		dictionary2["Banned_Close"] = "Close";
		dictionary2["Macros_Title"] = "Macro check";
		dictionary2["Macros_Start"] = "Start";
		dictionary2["Macros_Stop"] = "Stop";
		dictionary2["Macros_Clear"] = "Clear";
		dictionary2["Macros_Close"] = "Close";
		dictionary2["Macros_Hint"] = "Hold LMB and move the mouse across the field";
		dictionary2["Emulate_Title"] = "Automatic in-game check";
		dictionary2["Emulate_Start"] = "Start";
		dictionary2["Emulate_Stop"] = "Stop";
		dictionary2["Emulate_Close"] = "Close";
		dictionary2["Btn_Ok"] = "OK";
		dictionary2["Btn_Cancel"] = "Cancel";
		dictionary2["Btn_Close"] = "Close";
		dictionary2["Title_Error"] = "Error";
		dictionary2["Title_Info"] = "Information";
		dictionary2["Msg_Error"] = "Error: {0}";
		dictionary2["Msg_InitError"] = "Initialization error: {0}";
		dictionary2["Msg_ExitConfirm"] = "Are you sure you want to close the checker?";
		dictionary2["Title_Exit"] = "Exit";
		dictionary2["Exit_YesBtn"] = "Yes, exit";
		dictionary2["Btn_Yes"] = "Yes";
		dictionary2["Btn_No"] = "No";
		dictionary2["Btn_Ok"] = "OK";
		dictionary2["Msg_Cancel"] = "Cancel";
		dictionary2["Title_BlockDb"] = "BlockDB";
		dictionary2["Msg_NvidiaNotFound"] = "NVIDIA Control Panel not found.";
		dictionary2["Title_Nvidia"] = "NVIDIA";
		dictionary2["Mouse_Detected"] = "Mouse detected: {0}";
		dictionary2["Mouse_Standard"] = "Mouse: standard (no control software)";
		dictionary2["Mouse_Error"] = "Mouse detection error.";
		dictionary2["Title_MouseDetect"] = "Mouse detection";
		dictionary2["Banned_WindowTitle"] = "Blocked";
		dictionary2["Banned_Header"] = "⛔ YOUR DEVICE IS BLOCKED";
		dictionary2["Banned_ReasonUnknown"] = "Ban reason unknown.";
		dictionary2["Banned_Date"] = "Ban date: {0}";
		dictionary2["Banned_EndDate"] = "Ban end date: {0}";
		dictionary2["Banned_IssuedBy"] = "Issued by: {0}";
		dictionary2["Banned_CanClose"] = "You can close this window.";
		dictionary2["Banned_Countdown"] = "Window closes in {0}...";
		dictionary2["BlockDb_WindowTitle"] = "BlockDB — ban check";
		dictionary2["BlockDb_Accounts"] = "ACCOUNTS";
		dictionary2["BlockDb_Waiting"] = "Waiting...";
		dictionary2["BlockDb_NoBansShort"] = "✅ No bans found";
		dictionary2["BlockDb_Details"] = "BAN DETAILS";
		dictionary2["BlockDb_ColStatus"] = "Status / Expires";
		dictionary2["BlockDb_ColIssued"] = "Issued";
		dictionary2["BlockDb_CloseBtn"] = "Close";
		dictionary2["BlockDb_CheckingAccounts"] = "Checking accounts...";
		dictionary2["BlockDb_Done"] = "Done";
		dictionary2["BlockDb_BansCount"] = "Bans: {0}";
		dictionary2["BlockDb_ProjectBan"] = "Project ban: {0}";
		dictionary2["BlockDb_HistoryOnly"] = "History: {0}, no project ban";
		dictionary2["BlockDb_Clean"] = "Clean";
		dictionary2["BlockDb_ErrorShort"] = "Error";
		dictionary2["BlockDb_CheckingIp"] = "Checking IP: {0}";
		dictionary2["BlockDb_IpRequest"] = "IP request";
		dictionary2["BlockDb_DetailsFor"] = "BAN DETAILS — {0}";
		dictionary2["SO_StatusFast"] = "✔ Fast search";
		dictionary2["SO_StatusSystem"] = "✔ System drive";
		dictionary2["SO_StatusDrives"] = "✔ Select drives";
		dictionary2["SO_StatusFolder"] = "✔ Folder selected";
		dictionary2["SO_DriveScanError"] = "Drive scan error:\n{0}";
		dictionary2["SO_NoFolders"] = "No folders selected for scanning.";
		dictionary2["SO_PickFolder"] = "Select a folder to scan";
		dictionary2["Emulate_WindowTitle"] = "In-game check";
		dictionary2["Emulate_Desc"] = "Presses all keys in the CS2 window.\nIf a macro is bound to a key — it will trigger.";
		dictionary2["Emulate_StartCheck"] = "Start check";
		dictionary2["Emulate_GameNotRunning"] = "CS2 is not running! Start the game and try again.";
		dictionary2["Emulate_CheckTitle"] = "Check";
		dictionary2["Emulate_Instructions"] = "For the check to work correctly, set in CS2 settings:\n\n• Display mode: WINDOWED or FULLSCREEN WINDOWED\n• Place this window ABOVE the game window\n\nThen press [OK]";
		dictionary2["Emulate_Done"] = "✓ Check complete!";
		dictionary2["Macros_Color"] = "Color:";
		dictionary2["Macros_CrosshairCS2"] = "CS2 crosshair";
		dictionary2["Macros_CrosshairModel"] = "Model crosshair";
		dictionary2["Msg_AlreadyRunning"] = "The application is already running.";
		dictionary2["Msg_SteamNotRunning"] = "Steam is not running! Start Steam and try again.";
		dictionary2["Profile_Hidden"] = "Private";
		dictionary2["Profile_Open"] = "Public";
		dictionary2["Profile_Friends"] = "Friends only";
		dictionary2["Vac_YesDays"] = "VAC: yes ({0} d.)";
		dictionary2["Vac_Yes"] = "VAC: yes";
		dictionary2["Ban_Permanent"] = "♾ Permanent";
		dictionary2["Ban_LiftedPerm"] = "✅ Lifted (perm.)";
		dictionary2["Ban_ActiveUntil"] = "\ud83d\udd34 Active until {0:dd.MM.yyyy HH:mm}";
		dictionary2["Ban_Lifted"] = "✅ Lifted {0:dd.MM.yyyy HH:mm}";
		dictionary2["Ban_Forever"] = "Forever";
		dictionary2["Ban_Minutes"] = "{0} min";
		dictionary2["AD_Days"] = "{0} d.";
		dictionary2["AD_Hours"] = "{0} h.";
		dictionary2["AD_Minutes"] = "{0} min.";
		dictionary2["AD_CleanDetected"] = "Cleaning detected";
		dictionary2["AD_ServiceOff"] = "Service disabled";
		dictionary2["AD_XoneRunning"] = "XONE RUNNING";
		dictionary2["AD_FirewallSusp"] = "Suspicious firewall";
		dictionary2["AD_ExLoader"] = "ExLoader: strings.txt file ({0} ago)";
		dictionary2["AD_DusmStopped"] = "DusmSvc (Data Usage) is stopped";
		dictionary2["AD_Process"] = "Process: {0}";
		dictionary2["AD_FirewallFew"] = "Few inbound rules ({0}) — possible cleaning";
		dictionary2["Msg_NoInternet"] = "No internet connection!";
		dictionary2["Search_ExportTitle"] = "File search report";
		dictionary2["Search_ExportCount"] = "Number of detected files: {0}";
		dictionary2["Steam_ProjectBan"] = "Project ban : {0}";
		dictionary2["Steam_ProjectBanBadge"] = "PROJECT BAN";
		dictionary2["Lang_WindowTitle"] = "Language selection";
		dictionary2["Lang_Russian"] = "Русский";
		dictionary2["Lang_English"] = "English";
		dictionary2["Settings_Sound"] = "Interface sound";
		dictionary2["Settings_Volume"] = "Volume: {0}%";
		dictionary2["AD_StartService"] = "Start service";
		dictionary2["AD_ServiceStarted"] = "Service started ✓";
		dictionary2["AD_ServiceStartFail"] = "Failed to start service";
		dictionary2["Mouse_OpenApp"] = "Open {0}";
		dictionary2["Mouse_NoApp"] = "Mouse control software not found (Logitech G HUB).";
		dictionary2["Act_Regex"] = "Regex";
		dictionary2["Act_LoadExecuted"] = "Load ExecutedProgramsList";
		dictionary2["DataUsage_StartPrompt"] = "DusmSvc (Data Usage) service is stopped.\nStart it?";
		dictionary2["DataUsage_Title"] = "Data Usage";
		dictionary2["Discord_Checking"] = "Checking PC for prohibited software";
		dictionary2["Discord_OpenSite"] = "Open website";
		dictionary2["Link_Website"] = "Website";
		dictionary2["Link_Discord"] = "Discord";
		dictionary2["Link_VK"] = "VK";
		dictionary2["FL_ChooseLanguage"] = "Choose language";
		dictionary2["FL_ChooseSettings"] = "Settings";
		dictionary2["FL_Discord"] = "Discord status (checking PC)";
		dictionary2["FL_SaveAndStart"] = "Save and start";
		dictionary2["Btn_Next"] = "Next";
		dictionary2["Btn_Back"] = "Back";
		EN = dictionary2;
	}
}
