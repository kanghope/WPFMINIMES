using System;
using System.Collections.Generic;
using System.Text;

namespace MiniMES.Infrastructure.Auth
{
    public static class UserSession
    {
        public static string? UserId { get; set; }
        public static string? UserName { get; set; }
        public static string? UserRole { get; set; }
        public static DateTime? LoginTime { get; set; }

        // 세션 만료 시간 설정 (예: 30분)
        private static readonly int SessionTimeoutMinutes = 30;

        public static bool IsLoggedIn => !string.IsNullOrEmpty(UserId) && !IsSessionExpired();

        // 세션이 만료되었는지 확인하는 로직
        public static bool IsSessionExpired()
        {
            if (LoginTime == null) return true;

            var elapsed = DateTime.Now - LoginTime.Value;
            return elapsed.TotalMinutes > SessionTimeoutMinutes;
        }

        // 로그아웃 시 초기화
        public static void Clear()
        {
            UserId = null;
            UserName = null;
            UserRole = null;
            LoginTime = null;
        }
    }
}
