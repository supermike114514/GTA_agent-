using System;
using System.Drawing;
using System.Windows.Forms;
using GTA;
using GTA.UI;
using LemonUI;
using LemonUI.Menus;

namespace GTA5BuddyTest
{
    /// <summary>
    /// 最小验证脚本：LemonUI 到底能不能在本机渲染出图形菜单。
    ///
    /// 为什么不继续手搓：已实测 C# 的 ContainerElement / DRAW_RECT / Sprite 三种绘制方式
    /// 全部只出文字不出图形；C++ 的 presentCallbackRegister 注册成功却从不触发。而
    /// LemonUI 走 Scaleform（游戏自己的 UI 系统），是 GTA V 生态里最成熟的图形菜单框架，
    /// 大概率能工作。先用最小代码验证，避免又在错误方向上堆代码。
    ///
    /// 按 F9 开关菜单。日志写到桌面，因为游戏目录在本机对普通用户令牌不可写。
    /// </summary>
    public class TestMain : Script
    {
        private readonly ObjectPool _pool = new ObjectPool();
        private readonly NativeMenu _menu;

        private static string LogPath
        {
            get
            {
                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                return System.IO.Path.Combine(desktop, "GTA5BuddyLemon.log");
            }
        }

        private static void Log(string message)
        {
            try
            {
                System.IO.File.AppendAllText(LogPath,
                    "[" + DateTime.Now.ToString("HH:mm:ss") + "] " + message + Environment.NewLine);
            }
            catch (Exception) { }
        }

        public TestMain()
        {
            Interval = 0;
            Tick += OnTick;
            KeyDown += OnKeyDown;

            Log("TestMain: 构造函数开始");

            _menu = new NativeMenu("GTA5Buddy 图形测试", "LemonUI 菜单");
            _menu.Add(new NativeItem("第一项", "选中后应显示蓝色高亮条"));
            _menu.Add(new NativeItem("第二项", "右侧应有箭头/角标"));
            _menu.Add(new NativeItem("中文测试", "微软雅黑 / 游戏字体"));
            _menu.Add(new NativeItem("第四项", "滚动条测试"));
            _menu.Add(new NativeItem("第五项", "ESC 或 F9 关闭"));

            _pool.Add(_menu);
            _menu.Visible = true;

            Log("TestMain: NativeMenu 已建立并加入 ObjectPool，Visible=true");
        }

        private void OnTick(object sender, EventArgs e)
        {
            try
            {
                _pool.Process();
            }
            catch (Exception ex)
            {
                Log("Tick 异常: " + ex.GetType().Name + " " + ex.Message);
            }
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (e.KeyCode == Keys.F9)
                {
                    _menu.Visible = !_menu.Visible;
                    Log("F9: 菜单可见性 = " + _menu.Visible);
                }
            }
            catch (Exception ex)
            {
                Log("KeyDown 异常: " + ex.GetType().Name + " " + ex.Message);
            }
        }
    }
}
