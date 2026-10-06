/*
	GTA5Buddy 图形面板 —— ScriptHookV ASI 插件

	为什么用 C++ 重写界面：

	  在 GTA V Enhanced 上，C# (ScriptHookVDotNet) 能拿到的所有脚本级图形绘制都不渲染。
	  逐项实测：ContainerElement、DRAW_RECT 原生、Sprite —— 全部只出文字、没有背景板，
	  而 TextElement 的文字是正常的。原因在于 SHVDN 拿不到 DirectX 的 Present 回调。

	  C++ ASI 可以通过 ScriptHookV 的 presentCallbackRegister 拿到这个回调，直接在画布上
	  绘制。NativeTrainer 与各类 Atlas 风格菜单都走这条路，它们在本机显示完全正常。

	绘制分工：
	  * 面板底板/高亮条/边框 —— ScriptHookV 的 createTexture + drawTexture（GDI+ 封装），
	    用一张 8x8 纯白 PNG 染色拉伸而成。
	  * 文字 —— 直接在该帧的 SwapChain 后备缓冲上建 GDI+ Graphics 来绘制。这样字体由
	    Windows 提供（微软雅黑），中文渲染有保证，也是 Atlas 类菜单文字清晰的来源。

	线程模型：
	  * Present 回调在渲染线程，只读菜单状态。
	  * 键盘回调在游戏输入线程，只写菜单状态（按回车只置一个"待执行"标记）。
	  * 真正的游戏动作在脚本线程执行 —— Present 回调里不能调用 native（SDK 明确说明）。
*/

#include <Windows.h>
#include <Shlobj.h>
#include <d3d11.h>
#include <dxgi.h>
#include <wrl/client.h>
#include <gdiplus.h>
#include <string>
#include <vector>

#include "main.h"
#include "natives.h"

#pragma comment(lib, "ScriptHookV.lib")
#pragma comment(lib, "d3d11.lib")
#pragma comment(lib, "dxgi.lib")
#pragma comment(lib, "gdiplus.lib")
#pragma comment(lib, "user32.lib")

using namespace Microsoft::WRL;

// ---------------------------------------------------------------------------
// 常量
// ---------------------------------------------------------------------------

// 面板布局（归一化屏幕坐标）
static const float PANEL_X = 0.030f;
static const float PANEL_Y = 0.070f;
static const float PANEL_W = 0.300f;
static const float ROW_H = 0.0315f;
static const float HEADER_H = 0.0550f;
static const float FOOTER_H = 0.0340f;
static const int   MAX_VISIBLE = 14;

// ---------------------------------------------------------------------------
// 菜单数据
// ---------------------------------------------------------------------------

enum ActionId
{
	ACT_NONE = 0,
	ACT_CLOSE,
	ACT_HP,
	ACT_ARMOR,
	ACT_AMMO,
	ACT_INF_ON,
	ACT_INF_OFF,
	ACT_CARBINE,
	ACT_RPG,
	ACT_WANTED,
	ACT_REPAIR,
	ACT_SPAWN_ZENTORNO,
	ACT_SPAWN_ADDER,
	ACT_SPAWN_POLICE,
	ACT_SPAWN_BUZZARD,
	ACT_SPAWN_TANK,
	ACT_SPAWN_BMX,
};

struct MenuItem
{
	std::string label;
	std::string value;
	int action;
};

static std::vector<MenuItem> g_items;
static volatile LONG g_menuVisible = 0;
static volatile LONG g_selected = 0;
static volatile LONG g_scroll = 0;
static volatile LONG g_pendingAction = ACT_NONE;
static volatile LONG g_itemVersion = 0;   // 每次重建列表时自增，Present 回调据此缓存

// 提示信息（脚本线程写，渲染线程读）
static CRITICAL_SECTION g_hintLock;
static std::string g_hintText;
static volatile LONG g_hintUntil = 0;

// GDI+ 资源
static ULONG_PTR g_gdiToken = 0;
static Gdiplus::Font *g_fontTitle = NULL;
static Gdiplus::Font *g_fontBody = NULL;
static Gdiplus::Font *g_fontSmall = NULL;

// 脚本线程写入的玩家状态快照，供渲染线程显示
static CRITICAL_SECTION g_statusLock;
static std::string g_statusText = "玩家状态：读取中";

// 插件所在目录
static int g_textureId = -1;   // 8x8 纯白贴图

// g_items 由脚本线程构建、渲染线程读取，用同一把锁保护，避免重建时读到半成品。
static CRITICAL_SECTION g_itemsLock;

// ---------------------------------------------------------------------------
// 文本工具：UTF-8 -> UTF-16（GDI+ 需要宽字符，源码是 UTF-8 无 BOM）
// ---------------------------------------------------------------------------

static std::wstring widen(const std::string &utf8)
{
	if (utf8.empty()) return std::wstring();
	int need = MultiByteToWideChar(CP_UTF8, 0, utf8.c_str(), (int)utf8.size(), NULL, 0);
	if (need <= 0) return std::wstring();
	std::wstring out;
	out.resize((size_t)need);
	MultiByteToWideChar(CP_UTF8, 0, utf8.c_str(), (int)utf8.size(), &out[0], need);
	return out;
}

// ---------------------------------------------------------------------------
// 诊断日志
//
// 游戏目录在本机对普通用户令牌不可写，而 ScriptHookVDotNet 的日志正是因此一直失败，
// 所以这里尝试多个位置，优先用户目录（一定可写），方便排查"面板不显示"这类问题。
// ---------------------------------------------------------------------------

static std::string g_logPath;

static void panel_log(const std::string &msg)
{
	// 首次调用时解析一个可写位置
	if (g_logPath.empty())
	{
		char profile[MAX_PATH] = { 0 };
		if (SUCCEEDED(SHGetFolderPathA(NULL, CSIDL_PROFILE, NULL, 0, profile)))
		{
			g_logPath = std::string(profile) + "\\Desktop\\GTA5BuddyPanel.log";
		}
		else
		{
			g_logPath = "GTA5BuddyPanel.log";
		}
	}

	FILE *f = NULL;
	if (fopen_s(&f, g_logPath.c_str(), "a") != 0 || !f) return;

	SYSTEMTIME st;
	GetLocalTime(&st);
	fprintf(f, "[%02d:%02d:%02d] %s\n", st.wHour, st.wMinute, st.wSecond, msg.c_str());
	fclose(f);
}

// 只在第一次成功取到 DC 时记录，避免每帧刷日志
static void panel_log_ok()
{
	static volatile LONG logged = 0;
	if (InterlockedCompareExchange(&logged, 1, 0) == 0)
	{
		panel_log("on_present: GetDC 成功，开始用 GDI+ 绘制");
	}
}

// ---------------------------------------------------------------------------
// 绘制：全部走 GDI+
//
// 最初是"底板用 ScriptHookV 的 drawTexture、文字用 GDI+"两套混用，结果什么都不显示。
// 原因是两者争用同一张后备缓冲：drawTexture 由 ScriptHookV 在 Present 时通过 D3D 立即
// 上下文执行，而 GDI+ 需要在同一张缓冲上 GetDC —— 同时持有必然冲突。
//
// 现在统一用 GDI+：矩形与文字都在同一个 Graphics 上绘制，不再依赖贴图。
// ---------------------------------------------------------------------------

static int g_scrW = 1920;
static int g_scrH = 1080;

// 归一化坐标 -> 像素。x/y 是左上角。
static void gdi_rect(Gdiplus::Graphics &g, float x, float y, float w, float h,
                     BYTE a, BYTE r, BYTE gg, BYTE b)
{
	Gdiplus::SolidBrush brush(Gdiplus::Color(a, r, gg, b));
	g.FillRectangle(&brush,
	                (INT)(x * g_scrW), (INT)(y * g_scrH),
	                (INT)(w * g_scrW), (INT)(h * g_scrH));
}

static void draw_panel(Gdiplus::Graphics &g, float x, float y, float w, float h)
{
	// 投影，让面板从画面里浮出来
	gdi_rect(g, x - 0.004f, y - 0.004f, w + 0.008f, h + 0.008f, 150, 0, 0, 0);
	// 面板主体：深色不透明
	gdi_rect(g, x, y, w, h, 247, 14, 18, 28);
	// 标题栏
	gdi_rect(g, x, y, w, HEADER_H, 255, 26, 58, 102);
	// 标题栏下方的强调线
	gdi_rect(g, x, y + HEADER_H, w, 0.0030f, 255, 90, 153, 242);
	// 四周细边框
	gdi_rect(g, x, y, w, 0.0020f, 215, 84, 120, 178);
	gdi_rect(g, x, y + h - 0.0020f, w, 0.0020f, 215, 84, 120, 178);
	gdi_rect(g, x, y, 0.0020f, h, 215, 84, 120, 178);
	gdi_rect(g, x + w - 0.0020f, y, 0.0020f, h, 215, 84, 120, 178);
}

// ---------------------------------------------------------------------------
// 绘制：文字（GDI+，直接画在 SwapChain 后备缓冲上）
// ---------------------------------------------------------------------------

static void gdi_text(Gdiplus::Graphics &g, float px, float py, int pxW, int pxH,
                     const std::string &utf8, Gdiplus::Font *font,
                     BYTE a, BYTE r, BYTE gg, BYTE b)
{
	if (!font || utf8.empty()) return;

	std::wstring w = widen(utf8);
	if (w.empty()) return;

	// 描边：先在八个方向画黑色，再画本体，保证任意背景上都能读
	Gdiplus::SolidBrush shadow(Gdiplus::Color(a, 0, 0, 0));
	Gdiplus::SolidBrush brush(Gdiplus::Color(a, r, gg, b));
	Gdiplus::StringFormat fmt;
	fmt.SetAlignment(Gdiplus::StringAlignmentNear);
	fmt.SetLineAlignment(Gdiplus::StringAlignmentNear);

	const float ox[8] = { -1.4f, 0.0f, 1.4f, 0.0f, -1.0f, 1.0f, -1.0f, 1.0f };
	const float oy[8] = { 0.0f, -1.4f, 0.0f, 1.4f, -1.0f, -1.0f, 1.0f, 1.0f };

	for (int i = 0; i < 8; i++)
	{
		Gdiplus::RectF rr(px + ox[i], py + oy[i], (Gdiplus::REAL)pxW, (Gdiplus::REAL)pxH);
		g.DrawString(w.c_str(), -1, font, rr, &fmt, &shadow);
	}

	Gdiplus::RectF rr(px, py, (Gdiplus::REAL)pxW, (Gdiplus::REAL)pxH);
	g.DrawString(w.c_str(), -1, font, rr, &fmt, &brush);
}

// ---------------------------------------------------------------------------
// Present 回调
// ---------------------------------------------------------------------------

// 只在关键节点各记录一次，避免每帧刷屏
static void log_once(const char *key, const std::string &msg)
{
	static std::vector<std::string> done;
	for (size_t i = 0; i < done.size(); i++) if (done[i] == key) return;
	done.push_back(key);
	panel_log(msg);
}

static void on_present(void *swapChainPtr)
{
	if (InterlockedCompareExchange(&g_menuVisible, 0, 0) == 0) return;

	log_once("present_enter", "on_present: 已被调用（面板处于打开状态）");

	IDXGISwapChain *swapChain = (IDXGISwapChain *)swapChainPtr;
	if (!swapChain) { log_once("no_swapchain", "on_present: swapChain 为空"); return; }

	// 取后备缓冲与 D3D 设备，才能拿到 HDC 交给 GDI+
	ComPtr<ID3D11Texture2D> backBuffer;
	HRESULT hrBuf = swapChain->GetBuffer(0, __uuidof(ID3D11Texture2D), &backBuffer);
	if (FAILED(hrBuf) || !backBuffer)
	{
		char buf[160];
		sprintf_s(buf, "on_present: GetBuffer(0) 失败 hr=0x%08X", (unsigned)hrBuf);
		log_once("getbuffer_fail", buf);
		return;
	}
	log_once("getbuffer_ok", "on_present: 后备缓冲已取得");

	ComPtr<ID3D11Device> device;
	backBuffer->GetDevice(&device);
	if (!device) { log_once("no_device", "on_present: GetDevice 失败"); return; }

	ComPtr<ID3D11DeviceContext> context;
	device->GetImmediateContext(&context);
	if (!context) { log_once("no_context", "on_present: GetImmediateContext 失败"); return; }

	// DXGI 表面 -> GDI 设备上下文。GetDC 定义在 IDXGISurface1 上，需要先 QueryInterface。
	ComPtr<IDXGISurface1> surface1;
	HRESULT hrAs = backBuffer.As(&surface1);
	if (FAILED(hrAs) || !surface1)
	{
		char buf[160];
		sprintf_s(buf, "on_present: IDXGISurface1 转换失败 hr=0x%08X（后备缓冲不可 GDI 访问）", (unsigned)hrAs);
		log_once("surface1_fail", buf);
		return;
	}
	log_once("surface1_ok", "on_present: IDXGISurface1 已取得");

	HDC hdc = NULL;
	HRESULT hrDc = surface1->GetDC(FALSE, &hdc);
	if (FAILED(hrDc) || !hdc)
	{
		char buf[160];
		sprintf_s(buf, "on_present: GetDC 失败 hr=0x%08X", (unsigned)hrDc);
		log_once("getdc_fail", buf);
		return;
	}
	log_once("getdc_ok", "on_present: GetDC 成功，开始用 GDI+ 绘制");

	// 整个绘制过程持有列表锁：脚本线程重建列表时不会让渲染线程读到半成品
	EnterCriticalSection(&g_itemsLock);

	// 计算面板像素尺寸
	const int screenW = GetSystemMetrics(SM_CXSCREEN);
	const int screenH = GetSystemMetrics(SM_CYSCREEN);
	g_scrW = screenW;
	g_scrH = screenH;

	const int visibleCount = (int)g_items.size() > MAX_VISIBLE ? MAX_VISIBLE : (int)g_items.size();
	const float panelH = HEADER_H + (visibleCount * ROW_H) + FOOTER_H + 0.008f;

	const int pxX = (int)(PANEL_X * screenW);
	const int pxY = (int)(PANEL_Y * screenH);
	const int pxW = (int)(PANEL_W * screenW);

	// 整块面板与文字都画在同一个 Graphics 上
	Gdiplus::Graphics g(hdc);
	g.SetSmoothingMode(Gdiplus::SmoothingModeAntiAlias);
	g.SetTextRenderingHint(Gdiplus::TextRenderingHintAntiAliasGridFit);

	// ---- 1. 面板底板 ----
	draw_panel(g, PANEL_X, PANEL_Y, PANEL_W, panelH);

	// 选中行高亮
	const int scroll = (int)g_scroll;
	for (int i = 0; i < visibleCount; i++)
	{
		const int index = scroll + i;
		if (index >= (int)g_items.size()) break;

		const float rowY = PANEL_Y + HEADER_H + (i * ROW_H);
		if (index == (int)g_selected)
		{
			gdi_rect(g, PANEL_X + 0.002f, rowY, PANEL_W - 0.004f, ROW_H, 242, 41, 97, 173);
			gdi_rect(g, PANEL_X + 0.002f, rowY, 0.0042f, ROW_H, 255, 102, 184, 255);
		}
	}

	// ---- 2. 文字 ----
	const int padX = (int)(0.010f * screenW);

	// 标题
	gdi_text(g, pxX + padX, pxY + (int)(0.010f * screenH), pxW, (int)(0.030f * screenH),
	         "GTA5Buddy 控制台", g_fontTitle, 255, 255, 240, 200);

	// 副标题：玩家状态
	{
		EnterCriticalSection(&g_statusLock);
		std::string status = g_statusText;
		LeaveCriticalSection(&g_statusLock);
		gdi_text(g, pxX + padX, pxY + (int)(0.032f * screenH), pxW, (int)(0.024f * screenH),
		         status, g_fontSmall, 235, 200, 215, 240);
	}

	// 行
	for (int i = 0; i < visibleCount; i++)
	{
		const int index = scroll + i;
		if (index >= (int)g_items.size()) break;

		const int rowTop = pxY + (int)((HEADER_H + (i * ROW_H)) * screenH);
		const int textY = rowTop + (int)(0.0065f * screenH);

		gdi_text(g, pxX + padX, textY, pxW, (int)(ROW_H * screenH),
		         g_items[index].label, g_fontBody,
		         255, 255, 255, 255);

		if (!g_items[index].value.empty())
		{
			// 角标靠右：按字符数粗略右移（GDI+ 里可精确测量，这里够用）
			const int est = (int)(0.0125f * screenW * (float)g_items[index].value.size() / 2.0f);
			gdi_text(g, pxX + pxW - padX - est, textY, pxW, (int)(ROW_H * screenH),
			         g_items[index].value, g_fontSmall,
			         235, 175, 205, 238);
		}
	}

	// 底部提示：分隔线 + 文案
	{
		const int footTop = pxY + (int)((HEADER_H + (visibleCount * ROW_H)) * screenH);
		gdi_rect(g, PANEL_X, PANEL_Y + HEADER_H + (visibleCount * ROW_H), PANEL_W, 0.0016f,
		         180, 84, 120, 178);

		std::string hint;
		if (GetTickCount() < (DWORD)InterlockedCompareExchange(&g_hintUntil, 0, 0))
		{
			EnterCriticalSection(&g_hintLock);
			hint = g_hintText;
			LeaveCriticalSection(&g_hintLock);
		}
		if (hint.empty())
		{
			hint = "↑↓ 选择    回车执行    F8 关闭";
		}

		gdi_text(g, pxX + padX, footTop + (int)(0.005f * screenH), pxW, (int)(0.026f * screenH),
		         hint, g_fontSmall, 240, 200, 215, 238);

		// 列表超出可见范围时画滚动条
		if ((int)g_items.size() > MAX_VISIBLE)
		{
			const float trackY = PANEL_Y + HEADER_H;
			const float trackH = visibleCount * ROW_H;
			gdi_rect(g, PANEL_X + PANEL_W - 0.006f, trackY, 0.0032f, trackH, 165, 51, 66, 92);

			float thumbH = trackH * ((float)MAX_VISIBLE / (float)g_items.size());
			if (thumbH < 0.014f) thumbH = 0.014f;
			const int maxScroll = (int)g_items.size() - MAX_VISIBLE;
			const float thumbY = trackY + (trackH - thumbH) *
			                     ((maxScroll > 0) ? ((float)scroll / (float)maxScroll) : 0.0f);
			gdi_rect(g, PANEL_X + PANEL_W - 0.006f, thumbY, 0.0032f, thumbH, 242, 102, 184, 255);
		}
	}

	LeaveCriticalSection(&g_itemsLock);

	surface1->ReleaseDC(NULL);
}

// ---------------------------------------------------------------------------
// 键盘输入（游戏输入线程）
// ---------------------------------------------------------------------------

static void ensure_visible(int selected)
{
	const int sel = selected;
	if (sel < (int)g_scroll) g_scroll = sel;
	if (sel >= (int)g_scroll + MAX_VISIBLE) g_scroll = sel - MAX_VISIBLE + 1;
	if (g_scroll < 0) g_scroll = 0;
}

static void on_keyboard(DWORD keycode, WORD repeats, BYTE scan, BOOL isExtended,
                        BOOL isWithAlt, BOOL wasDownBefore, BOOL isUpNow)
{
	if (isUpNow || wasDownBefore) return;   // 只处理新按下

	const int count = (int)g_items.size();

	switch (keycode)
	{
	case VK_F8:
		if (InterlockedCompareExchange(&g_menuVisible, 0, 0) != 0)
		{
			InterlockedExchange(&g_menuVisible, 0);
			panel_log("F8: 关闭面板");
		}
		else
		{
			InterlockedExchange(&g_menuVisible, 1);
			InterlockedExchange(&g_selected, 0);
			InterlockedExchange(&g_scroll, 0);
			panel_log("F8: 打开面板");
		}
		break;

	case VK_UP:
		if (InterlockedCompareExchange(&g_menuVisible, 0, 0) == 0 || count == 0) return;
		{
			int sel = (int)InterlockedCompareExchange(&g_selected, 0, 0);
			sel = (sel - 1 + count) % count;
			InterlockedExchange(&g_selected, sel);
			ensure_visible(sel);
		}
		break;

	case VK_DOWN:
		if (InterlockedCompareExchange(&g_menuVisible, 0, 0) == 0 || count == 0) return;
		{
			int sel = (int)InterlockedCompareExchange(&g_selected, 0, 0);
			sel = (sel + 1) % count;
			InterlockedExchange(&g_selected, sel);
			ensure_visible(sel);
		}
		break;

	case VK_RETURN:
		if (InterlockedCompareExchange(&g_menuVisible, 0, 0) == 0 || count == 0) return;
		{
			const int sel = (int)InterlockedCompareExchange(&g_selected, 0, 0);
			if (sel >= 0 && sel < count)
			{
				// 动作必须回到脚本线程执行，这里只置标记
				InterlockedExchange(&g_pendingAction, g_items[sel].action);
			}
		}
		break;

	case VK_ESCAPE:
		InterlockedExchange(&g_menuVisible, 0);
		break;

	default:
		break;
	}
}

// ---------------------------------------------------------------------------
// 菜单内容
// ---------------------------------------------------------------------------

static void add_item(const char *label, const char *value, int action)
{
	MenuItem it;
	it.label = label;
	it.value = (value ? value : "");
	it.action = action;
	g_items.push_back(it);
}

static void rebuild_items()
{
	EnterCriticalSection(&g_itemsLock);
	g_items.clear();
	add_item("玩家 · 回满血量", "执行", ACT_HP);
	add_item("玩家 · 回满护甲", "执行", ACT_ARMOR);
	add_item("玩家 · 全部武器弹药补满", "执行", ACT_AMMO);
	add_item("玩家 · 无限弹药", "开", ACT_INF_ON);
	add_item("玩家 · 无限弹药", "关", ACT_INF_OFF);
	add_item("玩家 · 发放 卡宾步枪", "9999", ACT_CARBINE);
	add_item("玩家 · 发放 RPG", "50", ACT_RPG);
	add_item("玩家 · 消除通缉", "执行", ACT_WANTED);
	add_item("载具 · 刷 桑托劳 Zentorno", "超级跑车", ACT_SPAWN_ZENTORNO);
	add_item("载具 · 刷 爱德 Adder", "超级跑车", ACT_SPAWN_ADDER);
	add_item("载具 · 刷 警车 Police", "紧急车辆", ACT_SPAWN_POLICE);
	add_item("载具 · 刷 秃鹰 Buzzard", "直升机", ACT_SPAWN_BUZZARD);
	add_item("载具 · 刷 犀牛坦克 Rhino", "军用车辆", ACT_SPAWN_TANK);
	add_item("载具 · 刷 BMX 自行车", "自行车", ACT_SPAWN_BMX);
	add_item("载具 · 修理当前载具", "执行", ACT_REPAIR);
	add_item("关闭面板", "F8", ACT_CLOSE);

	LeaveCriticalSection(&g_itemsLock);

	InterlockedIncrement(&g_itemVersion);
}

static void set_hint(const char *text)
{
	EnterCriticalSection(&g_hintLock);
	g_hintText = text;
	LeaveCriticalSection(&g_hintLock);
	InterlockedExchange(&g_hintUntil, (LONG)(GetTickCount() + 3000));
}

// ---------------------------------------------------------------------------
// 动作（脚本线程，可以安全调用 native）
// ---------------------------------------------------------------------------

static void act_health()
{
	Ped p = PLAYER::PLAYER_PED_ID();
	if (!ENTITY::DOES_ENTITY_EXIST(p)) return;
	ENTITY::SET_ENTITY_MAX_HEALTH(p, 200);
	ENTITY::SET_ENTITY_HEALTH(p, 200);
	set_hint("血量已回满");
}

static void act_armor()
{
	Ped p = PLAYER::PLAYER_PED_ID();
	if (!ENTITY::DOES_ENTITY_EXIST(p)) return;
	PED::SET_PED_ARMOUR(p, 100);
	set_hint("护甲已回满");
}

static void act_ammo()
{
	Ped p = PLAYER::PLAYER_PED_ID();
	if (!ENTITY::DOES_ENTITY_EXIST(p)) return;

	Hash weapon = 0;
	if (WEAPON::GET_CURRENT_PED_WEAPON(p, &weapon, TRUE))
	{
		WEAPON::SET_PED_AMMO(p, weapon, 9999);
	}
	set_hint("当前武器弹药已补满");
}

static void act_infinite_ammo(bool on)
{
	Ped p = PLAYER::PLAYER_PED_ID();
	if (!ENTITY::DOES_ENTITY_EXIST(p)) return;
	WEAPON::SET_PED_INFINITE_AMMO(p, on ? TRUE : FALSE, 0);
	WEAPON::SET_PED_INFINITE_AMMO_CLIP(p, on ? TRUE : FALSE);
	set_hint(on ? "无限弹药已开启" : "无限弹药已关闭");
}

static void act_give_weapon(Hash weapon, int ammo, const char *hint)
{
	Ped p = PLAYER::PLAYER_PED_ID();
	if (!ENTITY::DOES_ENTITY_EXIST(p)) return;
	WEAPON::GIVE_DELAYED_WEAPON_TO_PED(p, weapon, ammo, FALSE);
	WEAPON::SET_CURRENT_PED_WEAPON(p, weapon, TRUE);
	set_hint(hint);}

static void act_spawn_vehicle(Hash model, const char *label)
{
	Ped p = PLAYER::PLAYER_PED_ID();
	if (!ENTITY::DOES_ENTITY_EXIST(p)) return;

	// 模型必须先流式加载完成，否则会生成空壳
	STREAMING::REQUEST_MODEL(model);
	const DWORD start = GetTickCount();
	while (!STREAMING::HAS_MODEL_LOADED(model) && (GetTickCount() - start) < 4000)
	{
		WAIT(0);
	}

	if (!STREAMING::HAS_MODEL_LOADED(model))
	{
		set_hint("模型加载失败：本安装可能缺少该载具");
		return;
	}

	Vector3 pos = ENTITY::GET_ENTITY_COORDS(p, TRUE);
	Vector3 fwd = ENTITY::GET_ENTITY_FORWARD_VECTOR(p);
	pos.x += fwd.x * 6.0f;
	pos.y += fwd.y * 6.0f;
	pos.z += 0.5f;

	const float heading = ENTITY::GET_ENTITY_HEADING(p);
	Vehicle veh = VEHICLE::CREATE_VEHICLE(model, pos.x, pos.y, pos.z, heading, TRUE, TRUE);

	if (!ENTITY::DOES_ENTITY_EXIST(veh))
	{
		set_hint("生成失败");
		STREAMING::SET_MODEL_AS_NO_LONGER_NEEDED(model);
		return;
	}

	ENTITY::SET_ENTITY_AS_MISSION_ENTITY(veh, TRUE, TRUE);
	VEHICLE::SET_VEHICLE_ON_GROUND_PROPERLY(veh);
	VEHICLE::SET_VEHICLE_ENGINE_ON(veh, TRUE, TRUE, FALSE);
	PED::SET_PED_INTO_VEHICLE(p, veh, -1);
	STREAMING::SET_MODEL_AS_NO_LONGER_NEEDED(model);

	set_hint(label);
}

static void act_repair()
{
	Ped p = PLAYER::PLAYER_PED_ID();
	if (!ENTITY::DOES_ENTITY_EXIST(p)) return;
	if (!PED::IS_PED_IN_ANY_VEHICLE(p, FALSE)) { set_hint("你不在载具里"); return; }

	Vehicle veh = PED::GET_VEHICLE_PED_IS_IN(p, FALSE);
	if (!ENTITY::DOES_ENTITY_EXIST(veh)) { set_hint("找不到载具"); return; }

	VEHICLE::SET_VEHICLE_FIXED(veh);
	VEHICLE::SET_VEHICLE_ENGINE_HEALTH(veh, 1000.0f);
	VEHICLE::SET_VEHICLE_DIRT_LEVEL(veh, 0.0f);
	set_hint("载具已修好");
}

static void act_wanted_clear()
{
	PLAYER::CLEAR_PLAYER_WANTED_LEVEL(PLAYER::PLAYER_ID());
	PLAYER::SET_PLAYER_WANTED_LEVEL(PLAYER::PLAYER_ID(), 0, FALSE);
	PLAYER::SET_PLAYER_WANTED_LEVEL_NOW(PLAYER::PLAYER_ID(), FALSE);
	set_hint("通缉已消除");
}

static void run_action(int action)
{
	switch (action)
	{
	case ACT_HP: act_health(); break;
	case ACT_ARMOR: act_armor(); break;
	case ACT_AMMO: act_ammo(); break;
	case ACT_INF_ON: act_infinite_ammo(true); break;
	case ACT_INF_OFF: act_infinite_ammo(false); break;
	case ACT_CARBINE: act_give_weapon(0xBFEFFF6D, 9999, "已发放 卡宾步枪"); break;
	case ACT_RPG: act_give_weapon(0xB1CA77B1, 50, "已发放 RPG"); break;
	case ACT_WANTED: act_wanted_clear(); break;
	case ACT_REPAIR: act_repair(); break;
	case ACT_SPAWN_ZENTORNO: act_spawn_vehicle(0x2DC10F19, "已生成 桑托劳"); break;
	case ACT_SPAWN_ADDER: act_spawn_vehicle(0xB779A091, "已生成 爱德"); break;
	case ACT_SPAWN_POLICE: act_spawn_vehicle(0x79FBB0C5, "已生成 警车"); break;
	case ACT_SPAWN_BUZZARD: act_spawn_vehicle(0x2F03547F, "已生成 秃鹰直升机"); break;
	case ACT_SPAWN_TANK: act_spawn_vehicle(0x2EA68690, "已生成 犀牛坦克"); break;
	case ACT_SPAWN_BMX: act_spawn_vehicle(0x43779C54, "已生成 BMX"); break;
	case ACT_CLOSE: InterlockedExchange(&g_menuVisible, 0); break;
	default: break;
	}
}

// ---------------------------------------------------------------------------
// 脚本线程
// ---------------------------------------------------------------------------

static std::string module_dir()
{
	char buf[MAX_PATH] = { 0 };
	GetModuleFileNameA(NULL, buf, MAX_PATH);
	std::string path(buf);
	const size_t pos = path.find_last_of("\\/");
	return (pos == std::string::npos) ? std::string("") : path.substr(0, pos + 1);
}

static void init_gdiplus()
{
	Gdiplus::GdiplusStartupInput input;
	Gdiplus::GdiplusStartup(&g_gdiToken, &input, NULL);

	// 微软雅黑是 Windows 自带的中文字体，中文显示有保证
	g_fontTitle = new Gdiplus::Font(L"Microsoft YaHei", 15.0f, Gdiplus::FontStyleBold, Gdiplus::UnitPixel);
	g_fontBody  = new Gdiplus::Font(L"Microsoft YaHei", 14.0f, Gdiplus::FontStyleRegular, Gdiplus::UnitPixel);
	g_fontSmall = new Gdiplus::Font(L"Microsoft YaHei", 12.0f, Gdiplus::FontStyleRegular, Gdiplus::UnitPixel);
}

static void shutdown_gdiplus()
{
	delete g_fontTitle; g_fontTitle = NULL;
	delete g_fontBody;  g_fontBody = NULL;
	delete g_fontSmall; g_fontSmall = NULL;
	if (g_gdiToken) { Gdiplus::GdiplusShutdown(g_gdiToken); g_gdiToken = 0; }
}

void ScriptMain()
{
	InitializeCriticalSection(&g_hintLock);
	InitializeCriticalSection(&g_statusLock);
	InitializeCriticalSection(&g_itemsLock);

	panel_log("ScriptMain: 脚本线程已启动");

	init_gdiplus();
	panel_log("ScriptMain: GDI+ 已初始化 (微软雅黑)");

	rebuild_items();
	panel_log("ScriptMain: 菜单项已建立");

	// 注意：present / keyboard 回调已在 DllMain 中注册（SDK 文档的要求），这里不再重复注册。

	// createTexture 依赖 ScriptHookV 的 D3D 设备。脚本刚启动时该设备往往还没建立，
	// 一次性调用必然失败（实测返回 0），所以移进主循环里重试，成功为止。
	const std::string texPath = module_dir() + "GTA5BuddyWhite.png";
	DWORD lastTexTry = 0;
	bool texReported = false;

	while (true)
	{
		// ---- 贴图：持续重试直到 ScriptHookV 的渲染设备就绪 ----
		if (g_textureId < 0 && (GetTickCount() - lastTexTry) > 1000)
		{
			lastTexTry = GetTickCount();
			g_textureId = createTexture(texPath.c_str());
			if (g_textureId >= 0 && !texReported)
			{
				texReported = true;
				char buf[160];
				sprintf_s(buf, "ScriptMain: createTexture 成功 id=%d（ScriptHookV 渲染设备已就绪）", g_textureId);
				panel_log(buf);
			}
		}

		// ---- 键盘回调只置标记，真正的动作在这里执行 ----
		const int action = (int)InterlockedExchange(&g_pendingAction, ACT_NONE);
		if (action != ACT_NONE) run_action(action);

		// ---- 刷新玩家状态供面板显示 ----
		Ped p = PLAYER::PLAYER_PED_ID();
		if (ENTITY::DOES_ENTITY_EXIST(p))
		{
			char buf[160];
			sprintf_s(buf, "玩家  HP %d/%d    护甲 %d    通缉 %d",
			          ENTITY::GET_ENTITY_HEALTH(p),
			          ENTITY::GET_ENTITY_MAX_HEALTH(p),
			          PED::GET_PED_ARMOUR(p),
			          PLAYER::GET_PLAYER_WANTED_LEVEL(PLAYER::PLAYER_ID()));

			EnterCriticalSection(&g_statusLock);
			g_statusText = buf;
			LeaveCriticalSection(&g_statusLock);
		}

		WAIT(0);
	}
}

BOOL APIENTRY DllMain(HMODULE module, DWORD reason, LPVOID reserved)
{
	if (reason == DLL_PROCESS_ATTACH)
	{
		// SDK 文档明确要求：presentCallbackRegister / keyboardHandlerRegister 必须在
		// DllMain 里注册。之前放在脚本线程里注册，结果回调注册成功但从不触发 —— 这是
		// 面板一直不显示的根本原因。
		presentCallbackRegister(on_present);
		keyboardHandlerRegister(on_keyboard);

		scriptRegister(module, ScriptMain);
	}
	else if (reason == DLL_PROCESS_DETACH)
	{
		keyboardHandlerUnregister(on_keyboard);
		presentCallbackUnregister(on_present);
		shutdown_gdiplus();
		scriptUnregister(module);
	}
	return TRUE;
}
