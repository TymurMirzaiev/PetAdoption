export function getMyPosition() {
    return new Promise((resolve, reject) => {
        if (!navigator.geolocation) return reject('unsupported');
        navigator.geolocation.getCurrentPosition(
            pos => resolve({ lat: pos.coords.latitude, lng: pos.coords.longitude }),
            err => reject(err.message),
            { timeout: 10000, maximumAge: 600000 });
    });
}
