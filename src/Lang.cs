// 화면 글자 세 언어(한국어 · 일본어 · 영어)와 언어마다 다른 기본값
using System.Globalization;

static class Lang
{
    public static string Code = "ko";
    public static readonly string[] Codes = { "ko", "ja", "en" };
    public static readonly string[] Names = { "한국어", "日本語", "English" };

    public static string T(string ko, string ja, string en) { return Code == "ja" ? ja : Code == "en" ? en : ko; }

    public static string Detect()
    {
        var n = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        return n == "ko" ? "ko" : n == "ja" ? "ja" : "en";
    }
    public static int Index(string code) { int i = System.Array.IndexOf(Codes, code); return i < 0 ? 0 : i; }

    public static string Front(string c) { return c == "ja" ? "_前" : c == "en" ? "_front" : "_앞"; }
    public static string Back(string c) { return c == "ja" ? "_後" : c == "en" ? "_back" : "_뒤"; }

    // 이름 모양 칸: 저장은 늘 한국어 낱말로, 화면에는 고른 언어 낱말로
    static readonly string[] Keys = { "앞", "이름", "앞뒤", "번호", "방향", "뒤" };
    static string[] Tokens(string c)
    {
        if (c == "ja") return new[] { "頭", "名前", "前後", "番号", "左右", "末尾" };
        if (c == "en") return new[] { "prefix", "name", "frontback", "number", "side", "suffix" };
        return Keys;
    }
    public static string ShowFormat(string f)
    {
        var t = Tokens(Code);
        for (int i = 0; i < Keys.Length; i++) f = f.Replace("{" + Keys[i] + "}", "{" + t[i] + "}");
        return f;
    }
    public static string ReadFormat(string f)
    {
        foreach (var c in Codes)
        {
            var t = Tokens(c);
            for (int i = 0; i < Keys.Length; i++) f = f.Replace("{" + t[i] + "}", "{" + Keys[i] + "}");
        }
        return f;
    }
    public static string FormatHelp()
    {
        var t = Tokens(Code);
        return "{" + t[0] + "} {" + t[1] + "} {" + t[2] + "} {" + t[3] + "} {" + t[4] + "} {" + t[5] + "}";
    }

    public static string DefaultNames(string c)
    {
        if (c == "ja") return
"# 顔\r\n顔\r\n耳\r\n鼻\r\n頬染め\r\n顔の影\r\n" +
"# 目\r\n目\r\n白目\r\n瞳\r\nハイライト\r\n上まつげ\r\nまつげ\r\n下まつげ\r\n目尻\r\n二重\r\n眉毛\r\n" +
"# 口\r\n口\r\n上唇\r\n下唇\r\n口内\r\n歯\r\n舌\r\n" +
"# 髪\r\nハイライト\r\n前髪\r\n横髪\r\n後ろ髪\r\nアホ毛\r\n髪の影\r\n" +
"---\r\n" +
"# 体\r\n首\r\n体\r\n胸\r\n脇\r\nお腹\r\n腰\r\n" +
"# 腕・脚\r\n腕\r\n前腕\r\n脚\r\n太もも\r\nふくらはぎ\r\n" +
"# 手・足\r\n手\r\n手首\r\n足\r\n足首\r\n指\r\n親指\r\n人差し指\r\n中指\r\n薬指\r\n小指\r\n手のひら\r\n" +
"---\r\n" +
"# 服・小物\r\n服\r\n襟\r\n袖\r\nズボン\r\nリボン_髪\r\nリボン_脚\r\nネックレス\r\n靴\r\n";
        if (c == "en") return
"# Face\r\nFace\r\nEar\r\nNose\r\nBlush\r\nFace shadow\r\n" +
"# Eyes\r\nEye\r\nEye white\r\nIris\r\nHighlight\r\nUpper lash\r\nLash\r\nLower lash\r\nEye corner\r\nDouble eyelid\r\nEyebrow\r\n" +
"# Mouth\r\nMouth\r\nUpper lip\r\nLower lip\r\nMouth inside\r\nTeeth\r\nTongue\r\n" +
"# Hair\r\nHighlight\r\nFront hair\r\nSide hair\r\nBack hair\r\nAhoge\r\nHair shadow\r\n" +
"---\r\n" +
"# Body\r\nNeck\r\nBody\r\nChest\r\nArmpit\r\nBelly\r\nPelvis\r\n" +
"# Arms & legs\r\nArm\r\nForearm\r\nLeg\r\nThigh\r\nCalf\r\n" +
"# Hands & feet\r\nHand\r\nWrist\r\nFoot\r\nAnkle\r\nFinger\r\nThumb\r\nIndex finger\r\nMiddle finger\r\nRing finger\r\nPinky\r\nPalm\r\n" +
"---\r\n" +
"# Clothes & props\r\nClothes\r\nCollar\r\nSleeve\r\nPants\r\nRibbon_hair\r\nRibbon_leg\r\nNecklace\r\nShoes\r\n";
        return
"# 얼굴\r\n얼굴\r\n귀\r\n코\r\n볼터치\r\n얼굴 그림자\r\n" +
"# 눈\r\n눈\r\n눈 흰자\r\n눈동자\r\n하이라이트\r\n윗속눈썹\r\n속눈썹\r\n아랫속눈썹\r\n눈꼬리\r\n쌍꺼풀\r\n눈썹\r\n" +
"# 입\r\n입\r\n윗입술\r\n아랫입술\r\n입 안\r\n이빨\r\n혀\r\n" +
"# 머리\r\n하이라이트\r\n앞머리\r\n옆머리\r\n뒷머리\r\n더듬이\r\n머리 그림자\r\n" +
"---\r\n" +
"# 몸\r\n목\r\n몸\r\n가슴\r\n겨드랑이\r\n배\r\n골반\r\n" +
"# 팔다리\r\n팔\r\n팔뚝\r\n다리\r\n허벅지\r\n종아리\r\n" +
"# 손 발\r\n손\r\n손목\r\n발\r\n발목\r\n손가락\r\n엄지\r\n검지\r\n중지\r\n약지\r\n새끼\r\n손바닥\r\n" +
"---\r\n" +
"# 옷 · 소품\r\n옷\r\n카라\r\n소매\r\n바지\r\n리본_머리\r\n리본_다리\r\n목걸이\r\n신발\r\n";
    }

    // 목록이 어느 언어의 기본값과 그대로 같은지(언어를 바꿀 때 같이 바꿔도 되는지)
    public static bool IsDefaultNames(string text)
    {
        var a = Norm(text);
        foreach (var c in Codes) if (a == Norm(DefaultNames(c))) return true;
        return false;
    }
    static string Norm(string s) { return s.Replace("\r", "").Trim().TrimStart('﻿'); }
}
