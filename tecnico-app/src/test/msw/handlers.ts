import { http, HttpResponse } from 'msw'

const BASE_URL = 'http://localhost:5000/api/v1'

// Default handlers — individual tests override these with server.use(...) as needed.
export const handlers = [
  http.get(`${BASE_URL}/clients`, () =>
    HttpResponse.json({ items: [], totalCount: 0, page: 1, pageSize: 20 })
  ),
  http.get(`${BASE_URL}/quotes`, () =>
    HttpResponse.json({ items: [], totalCount: 0, page: 1, pageSize: 20 })
  ),
]
