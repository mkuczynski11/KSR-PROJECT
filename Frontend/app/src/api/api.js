const axios = require("axios").default;
const API_URL = "http://localhost:8080/api";

export async function getBooks() {
  console.log(`Fetching all books from ${API_URL}`);
  let res = await axios
    .get(`${API_URL}/warehouse/books`, {
      params: {},
    })
    .then((response) => {
      return response;
    });

  return res.data;
}

export async function getBook(id) {
  console.log(`Fetching one book from ${API_URL}`);
  let res = await axios
    .get(`${API_URL}/warehouse/books/${id}`, {
      params: {},
    })
    .then((response) => {
      return response;
    });

  return res.data;
}

export async function getBooksPrices() {
  console.log(`Fetching price for books from ${API_URL}`);
  let res = await axios
    .get(`${API_URL}/sales/books`, {
      params: {},
    })
    .then((response) => {
      return response;
    });

  return res.data;
}

export async function getBooksDiscounts() {
  console.log(`Fetching discounts for books from ${API_URL}`);
  let res = await axios
    .get(`${API_URL}/marketing/books`, {
      params: {},
    })
    .then((response) => {
      return response;
    });

  return res.data;
}

export async function getShippingMethods() {
  console.log(`Fetching all shipping methods from ${API_URL}`);
  let res = await axios
    .get(`${API_URL}/shipping/methods`, {
      params: {},
    })
    .then((response) => {
      return response;
    });

  return res.data;
}

export async function getShippingPrices() {
  console.log(`Fetching prices for shipping methods from ${API_URL}`);
  let res = await axios
    .get(`${API_URL}/shipping/price`, {
      params: {},
    })
    .then((response) => {
      return response;
    });

  return res.data;
}

export async function createOrder(order) {
  console.log(`Creating order for book ${order.book.name} ${API_URL}`);
  let res = await axios
    .post(
      `${API_URL}/contact/orders/create`,
      {
        BookID: order.book.id,
        BookName: order.book.name,
        BookQuantity: order.quantity,
        BookPrice: order.book.unitPrice,
        BookDiscount: order.book.discount,
        DeliveryMethod: order.deliveryMethod.method,
        DeliveryPrice: order.deliveryMethod.price,
      },
      {
        headers: {
          "Content-Type": "application/json",
        },
      }
    )
    .then((response) => {
      return response;
    });
  return res;
}

export async function confirmOrder(order) {
  console.log(`Confirming order ${order.id} ${API_URL}`);
  let res = await axios
    .post(
      `${API_URL}/contact/orders/${order.id}/confirm`,
      {},
      {
        headers: {
          "Content-Type": "application/json",
        },
      }
    )
    .then((response) => {
      return response;
    });
  return res;
}

export async function getOrderStatus(id) {
  console.log(`Fetching order ${id} status from ${API_URL}`);
  let res = await axios
    .get(`${API_URL}/contact/orders/${id}/status`, {
      params: {},
    })
    .then((response) => {
      return response;
    });
  console.log(res);
  return res.data;
}

export async function getUserCars(user) {
  console.log(`Fetching user cars from ${API_URL}`);
  let res = await axios
    .get(`${API_URL}/users/${user}/cars`, {
      params: {},
    })
    .then((response) => {
      return response;
    });

  return res.data;
}

export async function deleteUser(user) {
  let res = await axios
    .delete(`${API_URL}/users/${user}`, {
      params: {},
    })
    .then((response) => {
      return response;
    });

  return res.data;
}

export async function createUser(user) {
  let res = await axios
    .post(
      `${API_URL}/users`,
      {
        login: user.login,
        name: user.name,
        surname: user.surname,
        email: user.email,
        password: user.password,
        birthdate: user.birthdate,
      },
      {
        headers: {
          "Content-Type": "application/json",
        },
      }
    )
    .then((response) => {
      return response;
    });

  return res;
}

export async function editUser(user) {
  let res = await axios
    .put(
      `${API_URL}/users/${user.login}`,
      {
        name: user.name,
        surname: user.surname,
        email: user.email,
        password: user.password,
        birthdate: user.birthdate,
      },
      {
        headers: {
          "Content-Type": "application/json",
        },
      }
    )
    .then((response) => {
      return response;
    });

  return res;
}

const api = {
  getBooks,
  getBook,
  getBooksPrices,
  getBooksDiscounts,
  getShippingMethods,
  getShippingPrices,
  createOrder,
  getOrderStatus,
  getUserCars,
  deleteUser,
};

export default api;
