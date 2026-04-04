using System;
using System.Linq;
using KKDS.Helpers;
using KKDS.Models;

namespace KKDS.Services
{
    public static class YetkiServisi
    {
        public static bool RolIzniVar(string aktifRol, params string[] izinVerilenRoller)
        {
            if (string.IsNullOrEmpty(aktifRol)) return false;
            if (aktifRol.Equals(Roller.Yonetici, StringComparison.OrdinalIgnoreCase)) return true;
            return izinVerilenRoller.Any(r => r.Equals(aktifRol, StringComparison.OrdinalIgnoreCase));
        }

        public static void ViewModelKoruma(BaseViewModel vm, params string[] izinVerilenRoller)
        {
            var rol = OturumBilgisi.Instance.Rol;
            if (RolIzniVar(rol, izinVerilenRoller)) return;
            vm.YetkisizMod = true;
            vm.YetkisizAciklama = "Bu ekrana erişim yetkiniz bulunmamaktadır. Yalnızca yetkili roller bu bölümü kullanabilir.";
        }
    }
}
