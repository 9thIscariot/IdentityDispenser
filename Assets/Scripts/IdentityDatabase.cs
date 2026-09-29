using System;
using System.Collections.Generic;

/// <summary>
/// 사용자가 제공한 이미지 기준 12명, 189개 인격의 원본 목록입니다.
/// 이미지의 줄바꿈은 공백으로 정리했습니다. 등급 및 가중치는 사용하지 않습니다.
/// ID는 게임 내부 ID가 아닌 이 프로젝트의 고정 ID입니다.
/// 항목을 추가하거나 순서를 바꾸더라도 기존 ID를 변경하거나 재사용하지 마세요.
/// </summary>
public sealed class IdentityDatabase
{
    public static IdentityDatabase Default { get; } = new IdentityDatabase();

    public IReadOnlyList<IdentityData> Identities { get; } = Array.AsReadOnly(new[]
    {
        // 이상 (16)
        new IdentityData("yi-sang-001", "이상", "검계 살수"),
        new IdentityData("yi-sang-002", "이상", "개화 E.G.O::동백"),
        new IdentityData("yi-sang-003", "이상", "W사 3등급 정리 요원"),
        new IdentityData("yi-sang-004", "이상", "약지 점묘파 스튜던트"),
        new IdentityData("yi-sang-005", "이상", "로보토미 E.G.O::엄숙한 애도"),
        new IdentityData("yi-sang-006", "이상", "남부 리우 협회 3과"),
        new IdentityData("yi-sang-007", "이상", "N사 E.G.O::흉탄"),
        new IdentityData("yi-sang-008", "이상", "흑수 - 오 필두"),
        new IdentityData("yi-sang-009", "이상", "거미집 검지 아비"),
        new IdentityData("yi-sang-010", "이상", "LCE E.G.O::차원찢개"),
        new IdentityData("yi-sang-011", "이상", "남부 세븐 협회 6과"),
        new IdentityData("yi-sang-012", "이상", "어금니 사무소 해결사"),
        new IdentityData("yi-sang-013", "이상", "피쿼드호 일등 항해사"),
        new IdentityData("yi-sang-014", "이상", "남부 디에치 협회 4과"),
        new IdentityData("yi-sang-015", "이상", "LCE E.G.O::초롱"),
        new IdentityData("yi-sang-016", "이상", "LCB 수감자"),

        // 파우스트 (16)
        new IdentityData("faust-001", "파우스트", "쥐는 자"),
        new IdentityData("faust-002", "파우스트", "남부 세븐 협회 4과"),
        new IdentityData("faust-003", "파우스트", "로보토미 E.G.O::후회"),
        new IdentityData("faust-004", "파우스트", "검계 살수"),
        new IdentityData("faust-005", "파우스트", "멀티크랙 사무소 대표"),
        new IdentityData("faust-006", "파우스트", "LCE E.G.O::홍염살"),
        new IdentityData("faust-007", "파우스트", "흑수 - 묘 필두"),
        new IdentityData("faust-008", "파우스트", "동부 시 협회 3과"),
        new IdentityData("faust-009", "파우스트", "검지 수행자 [쪽지]"),
        new IdentityData("faust-010", "파우스트", "거미집 약지 제자"),
        new IdentityData("faust-011", "파우스트", "새벽 사무소 해결사"),
        new IdentityData("faust-012", "파우스트", "W사 2등급 정리 요원"),
        new IdentityData("faust-013", "파우스트", "살아남은 로보토미 직원"),
        new IdentityData("faust-014", "파우스트", "남부 츠바이 협회 4과"),
        new IdentityData("faust-015", "파우스트", "워더링하이츠 버틀러"),
        new IdentityData("faust-016", "파우스트", "LCB 수감자"),

        // 돈키호테 (15) - 이미지 업로드 예정 칸은 사용자 지정 이름으로 반영했습니다.
        new IdentityData("don-quixote-001", "돈키호테", "W사 3등급 정리 요원"),
        new IdentityData("don-quixote-002", "돈키호테", "남부 섕크 협회 5과 부장"),
        new IdentityData("don-quixote-003", "돈키호테", "중지 작은 아우"),
        new IdentityData("don-quixote-004", "돈키호테", "T사 3등급 징수직 직원"),
        new IdentityData("don-quixote-005", "돈키호테", "라만차랜드 실장"),
        new IdentityData("don-quixote-006", "돈키호테", "동부 섕크 협회 3과"),
        new IdentityData("don-quixote-007", "돈키호테", "로보토미 E.G.O::사랑과 증오의 이름으로"),
        new IdentityData("don-quixote-008", "돈키호테", "흑수 - 미"),
        new IdentityData("don-quixote-009", "돈키호테", "검지 대행자 - 개화 E.G.O::대행"),
        new IdentityData("don-quixote-010", "돈키호테", "오트쿠튀르:: 르누아르 브랜드 매니저"),
        new IdentityData("don-quixote-011", "돈키호테", "남부 시 협회 5과 부장"),
        new IdentityData("don-quixote-012", "돈키호테", "N사 중간 망치"),
        new IdentityData("don-quixote-013", "돈키호테", "로보토미 E.G.O::초롱"),
        new IdentityData("don-quixote-014", "돈키호테", "검계 살수"),
        new IdentityData("don-quixote-015", "돈키호테", "LCB 수감자"),

        // 료슈 (16)
        new IdentityData("ryoshu-001", "료슈", "흑운회 와카슈"),
        new IdentityData("ryoshu-002", "료슈", "료.고.파. 주방장"),
        new IdentityData("ryoshu-003", "료슈", "W사 3등급 정리 요원"),
        new IdentityData("ryoshu-004", "료슈", "에드가 가문 치프 버틀러"),
        new IdentityData("ryoshu-005", "료슈", "로보토미 E.G.O::적안·참회"),
        new IdentityData("ryoshu-006", "료슈", "흑수 - 묘"),
        new IdentityData("ryoshu-007", "료슈", "N사 E.G.O::경멸, 경외"),
        new IdentityData("ryoshu-008", "료슈", "홍원 방랑무사"),
        new IdentityData("ryoshu-009", "료슈", "로보토미 E.G.O::잔향·외로움"),
        new IdentityData("ryoshu-010", "료슈", "거미집의 검"),
        new IdentityData("ryoshu-011", "료슈", "오트쿠튀르:: 르누아르 신발관"),
        new IdentityData("ryoshu-012", "료슈", "남부 세븐 협회 6과"),
        new IdentityData("ryoshu-013", "료슈", "LCCB 대리"),
        new IdentityData("ryoshu-014", "료슈", "남부 리우 협회 4과"),
        new IdentityData("ryoshu-015", "료슈", "20구 유로지비"),
        new IdentityData("ryoshu-016", "료슈", "LCB 수감자"),

        // 뫼르소 (15)
        new IdentityData("meursault-001", "뫼르소", "W사 2등급 정리 요원"),
        new IdentityData("meursault-002", "뫼르소", "N사 큰 망치"),
        new IdentityData("meursault-003", "뫼르소", "R사 제 4무리 코뿔소팀"),
        new IdentityData("meursault-004", "뫼르소", "검계 우두머리"),
        new IdentityData("meursault-005", "뫼르소", "남부 디에치 협회 4과 부장"),
        new IdentityData("meursault-006", "뫼르소", "서부 섕크 협회 3과"),
        new IdentityData("meursault-007", "뫼르소", "동부 엄지 카포 III"),
        new IdentityData("meursault-008", "뫼르소", "라만차랜드 왕자"),
        new IdentityData("meursault-009", "뫼르소", "로보토미 E.G.O::호넷 [변조]"),
        new IdentityData("meursault-010", "뫼르소", "약지 아수파 스튜던트"),
        new IdentityData("meursault-011", "뫼르소", "남부 리우 협회 6과"),
        new IdentityData("meursault-012", "뫼르소", "장미스패너 공방 해결사"),
        new IdentityData("meursault-013", "뫼르소", "중지 작은 아우"),
        new IdentityData("meursault-014", "뫼르소", "데드레빗츠 보스"),
        new IdentityData("meursault-015", "뫼르소", "LCB 수감자"),

        // 홍루 (16) - 중복 첨부된 이미지는 한 번만 반영했습니다.
        new IdentityData("hong-lu-001", "홍루", "콩콩이파 두목"),
        new IdentityData("hong-lu-002", "홍루", "K사 3등급 적출직 직원"),
        new IdentityData("hong-lu-003", "홍루", "남부 디에치 협회 4과"),
        new IdentityData("hong-lu-004", "홍루", "20구 유로지비"),
        new IdentityData("hong-lu-005", "홍루", "마침표 사무소 대표"),
        new IdentityData("hong-lu-006", "홍루", "R사 제 4무리 순록팀"),
        new IdentityData("hong-lu-007", "홍루", "홍원 군주"),
        new IdentityData("hong-lu-008", "홍루", "거미집 약지 아비"),
        new IdentityData("hong-lu-009", "홍루", "S사 추노꾼"),
        new IdentityData("hong-lu-010", "홍루", "동부 섕크 협회 3과"),
        new IdentityData("hong-lu-011", "홍루", "흑운회 와카슈"),
        new IdentityData("hong-lu-012", "홍루", "남부 리우 협회 5과"),
        new IdentityData("hong-lu-013", "홍루", "W사 2등급 정리 요원"),
        new IdentityData("hong-lu-014", "홍루", "갈고리 사무소 해결사"),
        new IdentityData("hong-lu-015", "홍루", "송곳니 사냥 사무소 해결사"),
        new IdentityData("hong-lu-016", "홍루", "LCB 수감자"),

        // 히스클리프 (16) - 중복 첨부된 이미지는 한 번만 반영했습니다.
        new IdentityData("heathcliff-001", "히스클리프", "R사 제 4무리 토끼팀"),
        new IdentityData("heathcliff-002", "히스클리프", "로보토미 E.G.O::여우비"),
        new IdentityData("heathcliff-003", "히스클리프", "피쿼드호 작살잡이"),
        new IdentityData("heathcliff-004", "히스클리프", "남부 외우피 협회 3과"),
        new IdentityData("heathcliff-005", "히스클리프", "와일드헌트"),
        new IdentityData("heathcliff-006", "히스클리프", "마침표 사무소 해결사"),
        new IdentityData("heathcliff-007", "히스클리프", "흑운회 와카슈"),
        new IdentityData("heathcliff-008", "히스클리프", "W사 4등급 정리 요원 - CCA"),
        new IdentityData("heathcliff-009", "히스클리프", "흑수 - 유 필두"),
        new IdentityData("heathcliff-010", "히스클리프", "중지 작은 형님"),
        new IdentityData("heathcliff-011", "히스클리프", "거미집 엄지 제자"),
        new IdentityData("heathcliff-012", "히스클리프", "남부 시 협회 5과"),
        new IdentityData("heathcliff-013", "히스클리프", "N사 작은 망치"),
        new IdentityData("heathcliff-014", "히스클리프", "남부 세븐 협회 4과"),
        new IdentityData("heathcliff-015", "히스클리프", "멀티크랙 사무소 해결사"),
        new IdentityData("heathcliff-016", "히스클리프", "LCB 수감자"),

        // 이스마엘 (16)
        new IdentityData("ishmael-001", "이스마엘", "R사 제 4무리 순록팀"),
        new IdentityData("ishmael-002", "이스마엘", "남부 리우 협회 4과"),
        new IdentityData("ishmael-003", "이스마엘", "어금니 보트 센터 해결사"),
        new IdentityData("ishmael-004", "이스마엘", "피쿼드호 선장"),
        new IdentityData("ishmael-005", "이스마엘", "서부 츠바이 협회 3과"),
        new IdentityData("ishmael-006", "이스마엘", "흑운회 부조장"),
        new IdentityData("ishmael-007", "이스마엘", "가주 후보"),
        new IdentityData("ishmael-008", "이스마엘", "정사무소 대표"),
        new IdentityData("ishmael-009", "이스마엘", "거미집 중지 제자"),
        new IdentityData("ishmael-010", "이스마엘", "LCD 현장추리팀"),
        new IdentityData("ishmael-011", "이스마엘", "오트쿠튀르:: 르루주 부티크"),
        new IdentityData("ishmael-012", "이스마엘", "남부 시 협회 5과"),
        new IdentityData("ishmael-013", "이스마엘", "LCCB 대리"),
        new IdentityData("ishmael-014", "이스마엘", "로보토미 E.G.O::출렁임"),
        new IdentityData("ishmael-015", "이스마엘", "에드가 가문 버틀러"),
        new IdentityData("ishmael-016", "이스마엘", "LCB 수감자"),

        // 로쟈 (17)
        new IdentityData("rodion-001", "로쟈", "흑운회 와카슈"),
        new IdentityData("rodion-002", "로쟈", "장미스패너 공방 대표"),
        new IdentityData("rodion-003", "로쟈", "남부 디에치 협회 4과"),
        new IdentityData("rodion-004", "로쟈", "남부 리우 협회 4과 부장"),
        new IdentityData("rodion-005", "로쟈", "북부 제뱌찌 협회 3과"),
        new IdentityData("rodion-006", "로쟈", "라만차랜드 공주"),
        new IdentityData("rodion-007", "로쟈", "흑수 - 사"),
        new IdentityData("rodion-008", "로쟈", "로보토미 E.G.O::눈물로 벼려낸 검"),
        new IdentityData("rodion-009", "로쟈", "R사 제 4무리 순록팀"),
        new IdentityData("rodion-010", "로쟈", "약지 아수파 도슨트"),
        new IdentityData("rodion-011", "로쟈", "거미집 엄지 아비"),
        new IdentityData("rodion-012", "로쟈", "오트쿠튀르:: 수선실"),
        new IdentityData("rodion-013", "로쟈", "LCCB 대리"),
        new IdentityData("rodion-014", "로쟈", "N사 중간 망치"),
        new IdentityData("rodion-015", "로쟈", "남부 츠바이 협회 5과"),
        new IdentityData("rodion-016", "로쟈", "T사 2등급 징수직 직원"),
        new IdentityData("rodion-017", "로쟈", "LCB 수감자"),

        // 싱클레어 (15)
        new IdentityData("sinclair-001", "싱클레어", "검계 살수"),
        new IdentityData("sinclair-002", "싱클레어", "쥐어들 자"),
        new IdentityData("sinclair-003", "싱클레어", "남부 섕크 협회 4과 부장"),
        new IdentityData("sinclair-004", "싱클레어", "새벽 사무소 해결사"),
        new IdentityData("sinclair-005", "싱클레어", "북부 제뱌찌 협회 3과"),
        new IdentityData("sinclair-006", "싱클레어", "중지 작은 아우"),
        new IdentityData("sinclair-007", "싱클레어", "동부 엄지 솔다토 II"),
        new IdentityData("sinclair-008", "싱클레어", "흑수 - 유"),
        new IdentityData("sinclair-009", "싱클레어", "거미집 소지 제자"),
        new IdentityData("sinclair-010", "싱클레어", "남부 츠바이 협회 6과"),
        new IdentityData("sinclair-011", "싱클레어", "마리아치 보스"),
        new IdentityData("sinclair-012", "싱클레어", "로보토미 E.G.O::홍적"),
        new IdentityData("sinclair-013", "싱클레어", "어금니 보트 센터 해결사"),
        new IdentityData("sinclair-014", "싱클레어", "서부 츠바이 협회 3과"),
        new IdentityData("sinclair-015", "싱클레어", "LCB 수감자"),

        // 오티스 (15)
        new IdentityData("outis-001", "오티스", "남부 세븐 협회 6과 부장"),
        new IdentityData("outis-002", "오티스", "어금니 사무소 해결사"),
        new IdentityData("outis-003", "오티스", "로보토미 E.G.O::마탄"),
        new IdentityData("outis-004", "오티스", "워더링하이츠 치프 버틀러"),
        new IdentityData("outis-005", "오티스", "W사 3등급 정리 요원 팀장"),
        new IdentityData("outis-006", "오티스", "라만차랜드 이발사"),
        new IdentityData("outis-007", "오티스", "흑수 - 묘"),
        new IdentityData("outis-008", "오티스", "T사 3등급 강력징수직 직원"),
        new IdentityData("outis-009", "오티스", "LCA 우제트 섹션 3팀 팀장"),
        new IdentityData("outis-010", "오티스", "거미집 중지 아비"),
        new IdentityData("outis-011", "오티스", "검계 살수"),
        new IdentityData("outis-012", "오티스", "G사 부장"),
        new IdentityData("outis-013", "오티스", "남부 섕크 협회 4과"),
        new IdentityData("outis-014", "오티스", "약지 점묘파 스튜던트"),
        new IdentityData("outis-015", "오티스", "LCB 수감자"),

        // 그레고르 (16)
        new IdentityData("gregor-001", "그레고르", "G사 일등대리"),
        new IdentityData("gregor-002", "그레고르", "남부 츠바이 협회 4과"),
        new IdentityData("gregor-003", "그레고르", "쌍갈고리 해적단 부선장"),
        new IdentityData("gregor-004", "그레고르", "에드가 가문 승계자"),
        new IdentityData("gregor-005", "그레고르", "라만차랜드 신부"),
        new IdentityData("gregor-006", "그레고르", "불주먹 사무소 생존자"),
        new IdentityData("gregor-007", "그레고르", "흑수 - 사"),
        new IdentityData("gregor-008", "그레고르", "밤의 송곳 카피타노"),
        new IdentityData("gregor-009", "그레고르", "로보토미 E.G.O::램프"),
        new IdentityData("gregor-010", "그레고르", "LCE E.G.O::AEDD"),
        new IdentityData("gregor-011", "그레고르", "새벽 사무소 대표"),
        new IdentityData("gregor-012", "그레고르", "남부 리우 협회 6과"),
        new IdentityData("gregor-013", "그레고르", "료.고.파. 조수"),
        new IdentityData("gregor-014", "그레고르", "장미스패너 공방 해결사"),
        new IdentityData("gregor-015", "그레고르", "흑운회 부조장"),
        new IdentityData("gregor-016", "그레고르", "LCB 수감자"),
    });

    /// <summary>추첨된 항목을 제거해도 원본 목록에 영향을 주지 않는 복사본입니다.</summary>
    public List<IdentityData> CreateDrawPool(string sinnerName = null)
    {
        var pool = new List<IdentityData>();
        foreach (IdentityData identity in Identities)
        {
            if (string.IsNullOrEmpty(sinnerName) || identity.SinnerName == sinnerName)
                pool.Add(identity);
        }
        return pool;
    }
}
