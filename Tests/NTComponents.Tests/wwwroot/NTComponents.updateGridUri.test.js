import '../../../NTComponents/wwwroot/NTComponents.lib.module.js';

describe('NTComponents.updateGridUri', () => {
    const gridKeys = ['b-page', 'b-pageSize', 'b-sort'];
    const currentUri = () => window.location.pathname + window.location.search + window.location.hash;

    test('keeps other grids and unrelated keys from the address bar while replacing the grid keys', () => {
        history.replaceState({ navigationIndex: 3 }, '', '/orders?a-page=2&a-sort=Name%3Aasc&tab=open&b-page=1#details');

        // The grid's own copy of the URL is stale: its non-grid keys and fragment must not replace the address bar's.
        NTComponents.updateGridUri('http://localhost/orders?a-page=1&tab=old&b-page=3&b-sort=Date%3Adesc#stale', gridKeys);

        expect(currentUri()).toBe('/orders?a-page=2&a-sort=Name%3Aasc&tab=open&b-page=3&b-sort=Date%3Adesc#details');
        expect(history.state).toEqual({ navigationIndex: 3 });
    });

    test('removes grid keys the new URI no longer has, matching decoded names case-insensitively', () => {
        history.replaceState(null, '', '/orders?B-Sort=Date%3Adesc&b%2DpageSize=50&tag=a&tag=b');

        NTComponents.updateGridUri('http://localhost/orders?b-page=1', gridKeys);

        expect(currentUri()).toBe('/orders?b-page=1&tag=a&tag=b');
    });

    test('writes the grid keys where the first grid key was', () => {
        history.replaceState(null, '', '/orders?tab=open&b-page=1&b-sort=Id%3Aasc&a-page=2');

        NTComponents.updateGridUri('http://localhost/orders?b-page=3&b-sort=Date%3Adesc', gridKeys);

        expect(currentUri()).toBe('/orders?tab=open&b-page=3&b-sort=Date%3Adesc&a-page=2');
    });

    test('keeps unrelated keys that cannot be decoded', () => {
        history.replaceState(null, '', '/orders?bad%E0=1');

        NTComponents.updateGridUri('http://localhost/orders?b-page=2', gridKeys);

        expect(currentUri()).toBe('/orders?bad%E0=1&b-page=2');
    });
});
