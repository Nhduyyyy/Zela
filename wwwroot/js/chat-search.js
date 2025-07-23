const searchBox = document.getElementById("searchBox");
const friendListContainer = document.getElementById("friend-list");

let debounceTimer;

searchBox?.addEventListener("input", function () {
    const keyword = this.value.trim();
    clearTimeout(debounceTimer);

    debounceTimer = setTimeout(() => {
        // Nếu rỗng thì gọi load lại toàn bộ danh sách
        if (keyword === "") {
            loadAllFriends();
            return;
        }

        fetch(`/Friendship/FilterFriends?keyword=${encodeURIComponent(keyword)}`)
            .then(res => {
                if (res.redirected) {
                    window.location.href = res.url;
                    return null;
                }
                return res.text();
            })
            .then(html => {
                if (html && friendListContainer) {
                    friendListContainer.innerHTML = html;
                }
            })
            .catch(err => {
                console.error("Lỗi khi tìm bạn bè:", err);
            });
    }, 300);
});

function loadAllFriends() {
    fetch(`/Friendship/FilterFriends`)
        .then(res => {
            if (res.redirected) {
                window.location.href = res.url;
                return null;
            }
            return res.text();
        })
        .then(html => {
            if (html && friendListContainer) {
                friendListContainer.innerHTML = html;
            }
        })
        .catch(err => {
            console.error("Lỗi khi tải lại danh sách bạn bè:", err);
        });
}
